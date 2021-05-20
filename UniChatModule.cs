using Fleck;
using MetalServer.Modules.UniChat.Server.Chats;
using MetalServer.Modules.UniChat.Server.Message;
using MetalServer.Modules.UniChat.Server.Users;
using MetalServer.Modules.UniChat.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query.Chat;

namespace MetalServer.Modules.UniChat {

	[Module(Name = "uniChat")]
	public class UniChatModule : ModuleBase {

		private const int PORT = 6969, FILE_UPLOAD_KEY_LENGTH = 16, FILE_KEY_LENGTH = 16;

		private WebSocketServer server;

		private readonly List<User> onlineUsers = new List<User>();

		private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

		private static readonly Dictionary<string, string> logContexts = new Dictionary<string, string>();

		public static UniChatModule mainInstance;

		public static readonly ExpiringDictionary<string, string> uploadingFiles = new ExpiringDictionary<string, string>();

		protected override void Begin() {

			CommandListener listener = new UniChatListener();

			servant.Listen?.Invoke(listener);
			servant.CatchUp?.Invoke();

			server.Start(socket => {

				string ip = socket.ConnectionInfo.ClientIpAddress;

				socket.OnOpen = () => {

					if(onlineUsers.Find((user) => user.IP.Equals(ip)) != null) {

						socket.Close();

						socket = null;

						servant.Log($"Connection attempt denied (Duplicate IP {ip})", foreground: Servant.LogColors.RED);

					}

					else {

						servant.Log($"New client connected ({ip})", foreground: Servant.LogColors.GREEN, group: (newClientGroup) => {

							logContexts[ip] = newClientGroup;

						});

					}

				};

				if(socket != null) {

					socket.OnClose = () => {

						servant.Log($"Client disconnected ({ip})", foreground: Servant.LogColors.RED, context: logContexts[ip]);

						User disconnectedUser = onlineUsers.Find(user => user.IP.Equals(ip));

						if(disconnectedUser != null) {

							ChatRoomsController.LeaveAllChats(disconnectedUser);

							onlineUsers.Remove(disconnectedUser);

						}

					};

					socket.OnMessage = json => {

						servant.Log($"Received new message: {json}", context: logContexts[ip], group: (newDataGroup) => {
						
							Message message = null;

							try {

								message = JsonConvert.DeserializeObject<Message>(json);

							}

							catch(Exception e) {

								servant.Log($"Failed to parse message ({e.Message})", context: newDataGroup);

							}

							if(message != null) {

								if(message.core?.action != null) {

									switch(message.core.action) {

										case Actions.PING:

											Send(new Message() {

												core = new Message.Core() {

													action = Actions.PONG

												}

											}, socket);

											break;

										case Actions.LOGIN_ANONYMOUS:

											if(message.auth != null && !string.IsNullOrWhiteSpace(message.auth.username)) {

												string key = GenerateKey(User.USER_KEY_LENGTH);

												onlineUsers.Add(new User(User.UserTypes.ANONYMOUS, message.auth.username, key, ip, (string type, dynamic update) => {

													switch(type) {

														case Actions.ADD_CHAT_MESSAGE:

															Send(new Message() {

																core = new Message.Core() {

																	action = Actions.ADD_CHAT_MESSAGE

																},

																query = new Message.Query() {

																	chatMesage = update.message,
																	chat = new Chat(update.key)

																}

															}, socket);

															break;

														case Actions.ADD_CHAT_FILE:

															Send(new Message() {

																core = new Message.Core() {

																	action = Actions.ADD_CHAT_FILE

																},

																query = new Message.Query() {

																	file = new ChatFile() {

																		key = update.file.key,
																		name = update.file.name

																	},
																	chat = new Chat(update.key)

																}

															}, socket);

															break;

													}

												}));

												Send(new Message() {

													core = new Message.Core() {

														action = Actions.LOGIN_ANONYMOUS,
														key = key

													}

												}, socket);

											}

											else {

												SendError(true, "Specified name is invalid", socket);

											}

											break;

										default:

											if(message.core.key != null) {

												User user = onlineUsers.Find(entry => entry.Key.Equals(message.core.key));

												if(user != null) {

													switch(message.core.action) {

														case Actions.LIST_CHAT_ROOMS:

															Send(new Message() {

																core = new Message.Core() {

																	action = Actions.LIST_CHAT_ROOMS

																},

																query = new Message.Query() {

																	chatList = ChatRoomsController.GetChats(user)

																}

															}, socket);

															break;

														case Actions.LIST_ACCESSIBLE_CHAT_ROOMS:

															Send(new Message() {

																core = new Message.Core() {

																	action = Actions.LIST_ACCESSIBLE_CHAT_ROOMS

																},

																query = new Message.Query() {

																	chatList = ChatRoomsController.GetAccessibleChats(user)

																}

															}, socket);

															break;

														case Actions.LOGOUT:

															onlineUsers.Remove(user);

															Send(new Message() {

																core = new Message.Core() {

																	action = Actions.LOGOUT

																}

															}, socket);

															break;

														case Actions.CHANGE_ANONYMOUS_NAME:

															ChatRoomsController.IsUserInAnyChat(user, () => SendError(true, "The user needs to leave all chats before they can change their name", socket), () => {

																user.Name = message.auth.username;

																Send(new Message() {

																	core = new Message.Core() {

																		action = Actions.CHANGE_ANONYMOUS_NAME

																	},

																	auth = new Message.Auth() {

																		username = message.auth.username

																	}

																}, socket);

															});

															break;

														default:

															if(message.query != null) {

																switch(message.core.action) {

																	case Actions.GET_FILE_META: {

																			string key = message.query.file.key,
																					chatKey = message.query.file.chatKey;

																			ChatFile result = database.GetCollection<Chat>("chats").FindOne(chat => chat.key.Equals(chatKey))?.files?.Find(file => file.key.Equals(key));

																			if(result != null) {

																				Send(new Message() {

																					core = new Message.Core() {

																						action = Actions.GET_FILE_META

																					},

																					query = new Message.Query() {

																						file = new ChatFile() {

																							name = result.name,
																							key = result.key,
																							chatKey = chatKey,
																							extension = result.extension,
																							destination = message.query.file.destination

																						},

																					}

																				}, socket);

																			}

																			else {

																				SendError(false, "File couldn't be found", socket);

																			}

																			break;

																		}

																	case Actions.GET_FILE: {

																			string key = message.query.file.key,
																					chatKey = message.query.file.chatKey;

																			ChatFile result = database.GetCollection<Chat>("chats").FindOne(chat => chat.key.Equals(chatKey)).files.Find(file => file.key.Equals(key));

																			if(result != null) {

																				result.destination = message.query.file.destination;
																				result.chatKey = chatKey;

																				Send(new Message() {

																					core = new Message.Core() {

																						action = Actions.GET_FILE

																					},

																					query = new Message.Query() {

																						file = result,

																					}

																				}, socket);

																			}

																			else {

																				SendError(false, "File couldn't be found", socket);

																			}

																			break;

																		}

																	case Actions.CREATE_CHAT_ROOM:

																		ChatRoomsController.CreateChatRoom(message.query.chat, (Chat chat) => {

																			onlineUsers.ForEach((onlineUser) => {

																				if(onlineUser.Type.Equals(User.UserTypes.ANONYMOUS)) {

																					Send(new Message() {

																						core = new Message.Core() {

																							action = Actions.ADD_CHAT_ROOM

																						},

																						query = new Message.Query() {

																							chat = new Chat(chat.key) {

																								name = chat.name,
																								description = chat.description,
																								messages = chat.messages,
																								password = string.IsNullOrEmpty(chat.password) ? ChatRoomsController.ChatRoomProtectoionStatus.UNLOCKED : ChatRoomsController.ChatRoomProtectoionStatus.LOCKED

																							}

																						}

																					}, socket);

																				}

																			});

																		}, () => SendError(true, "Failed to create chat", socket));

																		break;

																	case Actions.UPLOAD_FILE: {

																			string uploadKey = message.query.file?.key;

																			if(uploadKey != null && uploadingFiles.TryGetValue(uploadKey, out string chatKey)) {

																				string key = GenerateKey(FILE_KEY_LENGTH);

																				message.query.file.key = key;

																				uploadingFiles.Remove(uploadKey);

																				ChatRoomsController.UploadFile(chatKey, message.query.file, () => {

																				}, () => {

																					SendError(false, "Error uploading file", socket);

																				});

																			}

																			else {

																				SendError(true, "Error uploading file - wrong key", socket);

																			}

																			break;

																		}

																	default:

																		if(message.query.chat?.key != null) {

																			switch(message.core.action) {

																				case Actions.START_FILE_UPLOAD:

																					ChatRoomsController.IsUserInChat(message.query.chat.key, user, () => {

																						string key = GenerateKey(FILE_UPLOAD_KEY_LENGTH);

																						uploadingFiles.Add(key, message.query.chat?.key, 1 * 60 * 1000);

																						Send(new Message() {

																							core = new Message.Core() {

																								action = Actions.START_FILE_UPLOAD

																							},

																							query = new Message.Query() {

																								file = new ChatFile() {

																									key = key,
																									name = message.query.file.name

																								}

																							}

																						}, socket);

																					}, () => {

																						SendError(true, "User can't upload files to this chat", socket);

																					});

																					break;

																				case Actions.LEAVE_CHAT_ROOM:

																					ChatRoomsController.LeaveChat(message.query.chat.key, user, () => {

																						Send(new Message() {

																							core = new Message.Core() {

																								action = Actions.LEAVE_CHAT_ROOM

																							},

																							query = new Message.Query() {

																								chat = new Chat(message.query.chat.key)

																							}

																						}, socket);

																						ChatRoomsController.ChatExists(message.query.chat.key, null, () => {

																							Send(new Message() {

																								core = new Message.Core() {

																									action = Actions.REMOVE_CHAT_ROOM

																								},

																								query = new Message.Query() {

																									chat = new Chat(message.query.chat.key)

																								}

																							}, socket);

																						});

																					}, () => SendError(true, "This user is not connected to a chat", socket));

																					break;

																				case Actions.ADD_CHAT_MESSAGE:

																					if(message.query.chatMesage?.text != null) {

																						ChatRoomsController.SendMessage(message.query.chat.key, message.query.chatMesage, ChatMessage.ChatMessageTypes.BASIC, user, () => {

																						}, () => SendError(true, "Chat doesn't exist or user have not joined it", socket));

																					}

																					else {

																						SendError(true, "Message can't be empty", socket);

																					}

																					break;

																				case Actions.GET_CHAT_MESSAGES:

																					ChatRoomsController.GetChatMessages(message.query.chat.key, (List<ChatMessage> chatMessages) => Send(new Message() {

																						core = new Message.Core() {

																							action = Actions.GET_CHAT_MESSAGES

																						},

																						query = new Message.Query() {

																							chat = new Chat(message.query.chat.key) {

																								messages = chatMessages

																							}

																						}

																					}, socket), () => SendError(true, "Chat with specified key was not found", socket));

																					break;

																				case Actions.GET_CHAT_FILES:

																					ChatRoomsController.GetChatFiles(message.query.chat.key, (List<ChatFile> chatFiles) => Send(new Message() {

																						core = new Message.Core() {

																							action = Actions.GET_CHAT_FILES

																						},

																						query = new Message.Query() {

																							chat = new Chat(message.query.chat.key) {

																								files = chatFiles

																							}

																						}

																					}, socket), () => SendError(true, "Chat with specified key was not found", socket));

																					break;

																				case Actions.UNLOCK_CHAT_ROOM:

																					ChatRoomsController.UnlockChat(message.query.chat?.key, user, message.query.chat?.password, () => {

																						Send(new Message() {

																							core = new Message.Core() {

																								action = Actions.UNLOCK_CHAT_ROOM

																							},

																							query = new Message.Query() {

																								chat = new Chat(message.query.chat?.key)

																							}

																						}, socket);

																					}, () => SendError(true, "Failed to unlock chat", socket));

																					break;

																				case Actions.JOIN_CHAT_ROOM:

																					ChatRoomsController.JoinChat(message.query.chat?.key, user, (Chat chat) => {

																						Send(new Message() {

																							core = new Message.Core() {

																								action = Actions.JOIN_CHAT_ROOM

																							},

																							query = new Message.Query() {

																								chat = chat

																							}

																						}, socket);

																					}, () => SendError(true, "Failed to join chat", socket));

																					break;

																			}

																		}

																		else {

																			SendError(true, "No chat key specified", socket);

																		}

																		break;

																}

															}

															else {

																SendError(true, "No query specified", socket);

															}

															break;

													}

												}

												else {

													SendError(true, "This user is no longer online", socket);

												}

											}

											else {

												SendError(true, "No key specified", socket);

											}

											break;

									}

								}

								else {

									SendError(true, "No action specified", socket);

								}

							} else {

								SendError(true, "Unable to parse message", socket);

							}

						});

					};

				}

			});

			servant.Log("Server started", foreground: Servant.LogColors.MAGENTA);

			IsRunning = true;

		}

		protected override bool Setup() {

			mainInstance = this;

			server = new WebSocketServer($"ws://0.0.0.0:{PORT}");

			ChatRoomsController.Init();

			IsInitialized = true;

			return IsInitialized;

		}

		protected override void Stop() {

			server.Dispose();

			IsRunning = false;

		}

		public static string GenerateKey(int length) {

			byte[] bytes = new byte[length];

			rngCsp.GetBytes(bytes);

			return Convert.ToBase64String(bytes);

		}

		private void Send(Message message, IWebSocketConnection socket) {

			string json = JsonConvert.SerializeObject(message, new JsonSerializerSettings() {
			
				NullValueHandling = NullValueHandling.Ignore

			});

			socket.Send(json);

		}

		private void SendError(bool clientOtherwiseServer, string text, IWebSocketConnection socket) {

			Send(new Message() {

				core = new Message.Core() {

					action = Actions.ERROR

				},

				error = new Message.Error() {

					type = clientOtherwiseServer ? Message.Error.ErrorTypes.CLIENT : Message.Error.ErrorTypes.SERVER,
					message = text

				}

			}, socket);

		}

		private class UniChatListener : CommandListener {

			public override bool Command(Command command) {

				switch(command.Name) {

					//

				}

				return true;

			}

		}

	}

}