using Godot;
using System;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities;


/**
 * Script attached to the Controller object in the scene.
 */
public partial class GameManager : Control
{


    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------

    [ExportCategory("UI Settings")]

    [Export]
    public LineEdit messageInput;
    [Export]
    public RichTextLabel chatTextArea;
    [Export]
    public Control leavePopup;

    private SmartFox sfs;
    private GlobalManager global;

    private bool runTimer;
    private float timer = 20f;
    private string lastSenderName;

    //----------------------------------------------------------
    // Callback Methods
    //----------------------------------------------------------
    #region
    public override void _Ready()
    {

        global = (GlobalManager)GetNode("/root/globalmanager");
        sfs = global.GetSfsClient();

        // Add event listeners
        AddSmartFoxListeners();

        PrintSystemMessage("Game joined as " + (sfs.MySelf.IsPlayer ? "player" : "spectator"));



        // If user is the first player in the Room, set a timeout
        // Having a timeout is usefult to suggest the user to leave the game if not yet started within some time
        // For example this could mean that the invited buddy refused the invitation, or the server couldn't locate other players to invite

        if (sfs.MySelf.IsPlayer && sfs.LastJoinedRoom.PlayerList.Count == 1)
            runTimer = true;

    }

    public override void _Process(double delta)
    {
        // Process the SmartFox events queue
        if (sfs != null)
            sfs.ProcessEvents();

        OnMessageInputEndEdit();

        if (runTimer)
        {
            timer -= (float)delta;

            int timerInt = (int)Math.Round(timer, 0);

            if (timerInt == 0)
                StopTimeout(true);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (sfs != null && sfs.IsConnected)
                sfs.Disconnect();

            GD.Print("Application Quit");
        }
    }
    #endregion

    //----------------------------------------------------------
    // UI event listeners
    //----------------------------------------------------------
    #region



    /**
        * On public chat message input edit end, if the Enter key was pressed, send the chat message.
        */
    public void OnMessageInputEndEdit()
    {
        if (Input.IsActionJustPressed("ui_accept") && !Input.IsActionJustPressed("ui_select"))

            SendMessage();
    }

    /**
	 * On Send button click, send the chat message.
	 */
    public void OnSendButtonClick()
    {
        SendMessage();
    }

    /**
	 * On Leave button click, go back to Login scene.
	 */
    public void OnLeaveButtonClick()
    {
        // Leave current game room
        sfs.Send(new LeaveRoomRequest());

        // Return to lobby scene
        RemoveSmartFoxListeners();
        GetNode<Control>("Leave Panel").Hide();
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("lobby.tscn");
    }

    public void OnLeavePanelShow()
    {
        leavePopup.Position = (Vector2I)((GetViewportRect().Size - leavePopup.Size) / 2);
        GetTree().Paused = true;
        GetNode<Control>("Leave Panel").Show();
    }
    public void OnLeavePanelHide()
    {
        GetNode<Control>("Leave Panel").Hide();
        GetTree().Paused = false;
    }
    #endregion

    //----------------------------------------------------------
    // Helper methods
    //----------------------------------------------------------
    #region
    private void AddSmartFoxListeners()
    {

        sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
        sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
        sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);

    }

    /**
	 * Remove all SmartFoxServer-related event listeners added by the scene.
	 */
    private void RemoveSmartFoxListeners()
    {
        sfs.RemoveEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
        sfs.RemoveEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
        sfs.RemoveEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);

    }

    private void StopTimeout(bool showPanel)
    {
        runTimer = false;

        if (showPanel)
        {
            OnLeavePanelShow();
        }
    }

    /**
	 * Send a public chat message.
	 */
    private void SendMessage()
    {
        if (messageInput.Text != "")
        {
            // Send public message to Room
            sfs.Send(new PublicMessageRequest(messageInput.Text));

            // Reset message input
            messageInput.Text = "";
            messageInput.Select();
        }
    }

    /**
	 * Display a chat message.
	 */
    private void PrintChatMessage(string message, string senderName)
    {
        // Print sender name, unless they are the same of the last message
        if (senderName != lastSenderName)
        {
            chatTextArea.Text += "[b]" + (senderName == "" ? "Me" : senderName) + "[/b]\n";

        }

        // Print chat message
        chatTextArea.Text += message + "\n";

        // Save reference to last message sender, to avoid repeating the name for subsequent messages from the same sender
        lastSenderName = senderName;

    }

    /**
	 * Display a system message.
	 */
    private void PrintSystemMessage(string message)
    {
        // Print message

        //     TextEdit textEdit = new TextEdit();
        chatTextArea.Text += "[color=white][i]" + message + "[/i][/color]\n";


    }



    #endregion

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------
    #region
    private void OnPublicMessage(BaseEvent evt)
    {
        User sender = (User)evt.Params["sender"];
        string message = (string)evt.Params["message"];

        // Display chat message
        PrintChatMessage(message, sender != sfs.MySelf ? sender.Name : "");
    }

    private void OnUserEnterRoom(BaseEvent evt)
    {
        User user = (User)evt.Params["user"];
        Room room = (Room)evt.Params["room"];

        // Display system message
        PrintSystemMessage("User " + user.Name + " joined this game as " + (user.IsPlayerInRoom(room) ? "player" : "spectator"));

        // Stop timeout
        if (user.IsPlayerInRoom(room))
            StopTimeout(false);
    }

    private void OnUserExitRoom(BaseEvent evt)
    {
        User user = (User)evt.Params["user"];

        // Display system message
        if (user != sfs.MySelf)
            PrintSystemMessage("User " + user.Name + " left the game");
    }

    #endregion
}