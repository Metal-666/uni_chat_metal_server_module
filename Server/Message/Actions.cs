namespace MetalServer.Modules.UniChat.Server.Message {

	public sealed class Actions {

		public const string

		#region Testing

		PING = "PING",
		PONG = "PONG",

		#endregion

		#region Errors

		ERROR = "ERROR",

		#endregion

		#region Login

		LOGIN_ANONYMOUS = "LOGIN_ANONYMOUS",
		LOGIN_GOOGLE = "LOGIN_GOOGLE",

		#endregion

		#region Logout

		LOGOUT = "LOGOUT",

		#endregion

		#region ChatRoom

		LIST_CHAT_ROOMS = "LIST_CHAT_ROOMS",
		LIST_ACCESSIBLE_CHAT_ROOMS = "LIST_ACCESSIBLE_CHAT_ROOMS",
		GET_CHAT_MESSAGES = "GET_CHAT_MESSAGES",
		GET_CHAT_FILES = "GET_CHAT_FILES",
		JOIN_CHAT_ROOM = "JOIN_CHAT_ROOM",
		LEAVE_CHAT_ROOM = "LEAVE_CHAT_ROOM",
		ADD_CHAT_MESSAGE = "ADD_CHAT_MESSAGE",
		ADD_CHAT_FILE = "ADD_CHAT_FILE",
		REMOVE_CHAT_ROOM = "REMOVE_CHAT_ROOM",
		CREATE_CHAT_ROOM = "CREATE_CHAT_ROOM",
		ADD_CHAT_ROOM = "ADD_CHAT_ROOM",
		UNLOCK_CHAT_ROOM = "UNLOCK_CHAT_ROOM",

		#endregion

		#region Account

		CHANGE_ANONYMOUS_NAME = "CHANGE_ANONYMOUS_NAME",

		#endregion

		#region Files

		START_FILE_UPLOAD = "START_FILE_UPLOAD",
		UPLOAD_FILE = "UPLOAD_FILE",
		GET_FILE = "GET_FILE",
		GET_FILE_META = "GET_FILE_META";

		#endregion

	}

}