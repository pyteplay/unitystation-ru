﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

/// <summary>
/// The admin chat relay
/// </summary>
public partial class PlayerList
{
    public Dictionary<string, List<AdminChatMessage>> adminChatInbox
	    = new Dictionary<string, List<AdminChatMessage>>();

    public List<AdminChatMessage> CheckAdminInbox(string adminUserID)
    {
	    var list = new List<AdminChatMessage>();

	    if (!adminChatInbox.ContainsKey(adminUserID)) return list;

	    return adminChatInbox[adminUserID];
    }
}
