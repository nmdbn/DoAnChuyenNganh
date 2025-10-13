/**
 * Chat Manager - Handles real-time chat using SignalR
 */
const ChatManager = (function () {
  let connection = null;
  let userId = null;
  let currentConversationUserId = null;
  let isInitialized = false;

  /**
   * Initialize the chat system
   */
  function init(currentUserId) {
    if (isInitialized) {
      console.warn("ChatManager already initialized");
      return;
    }

    userId = currentUserId;
    setupSignalR();
    isInitialized = true;

    console.log("ChatManager initialized for user:", userId);
  }

  /**
   * Setup SignalR connection
   */
  function setupSignalR() {
    connection = new signalR.HubConnectionBuilder()
      .withUrl("/chatHub")
      .withAutomaticReconnect()
      .build();

    // Handle incoming messages
    connection.on("ReceiveMessage", function (message) {
      console.log("Received message:", message);
      handleNewMessage(message);
    });

    // Handle messages read confirmation
    connection.on("MessagesRead", function (readByUserId) {
      console.log("Messages read by user:", readByUserId);
      markMessagesAsReadInUI(readByUserId);
    });

    // Handle typing indicator
    connection.on("UserTyping", function (typingUserId) {
      console.log("User typing:", typingUserId);
      if (currentConversationUserId === typingUserId) {
        showTypingIndicator();
      }
    });

    // Handle stopped typing
    connection.on("UserStoppedTyping", function (typingUserId) {
      console.log("User stopped typing:", typingUserId);
      if (currentConversationUserId === typingUserId) {
        hideTypingIndicator();
      }
    });

    // Handle user online status
    connection.on("UserOnline", function (onlineUserId) {
      console.log("User online:", onlineUserId);
      updateUserOnlineStatus(onlineUserId, true);
    });

    // Handle user offline status
    connection.on("UserOffline", function (offlineUserId) {
      console.log("User offline:", offlineUserId);
      updateUserOnlineStatus(offlineUserId, false);
    });

    // Handle online users count update
    connection.on("UpdateOnlineUsersCount", function (count) {
      console.log("Online users count:", count);
      updateOnlineUsersCount(count);
    });

    // Handle unread count update
    connection.on("UpdateUnreadCount", function (count) {
      console.log("Unread messages count:", count);
      updateUnreadBadge(count);
    });

    // Handle errors
    connection.on("Error", function (errorMessage) {
      console.error("Chat error:", errorMessage);
      showError(errorMessage);
    });

    // Handle reconnection
    connection.onreconnecting((error) => {
      console.warn("Connection lost. Reconnecting...", error);
    });

    connection.onreconnected((connectionId) => {
      console.log("Reconnected with connection ID:", connectionId);
    });

    connection.onclose((error) => {
      console.error("Connection closed:", error);
    });

    // Start connection
    startConnection();
  }

  /**
   * Start SignalR connection
   */
  async function startConnection() {
    try {
      await connection.start();
      console.log("ChatHub connected successfully");

      // Request initial data
      try {
        await connection.invoke("RequestOnlineUsersCount");
        console.log("Requested online users count");
      } catch (err) {
        console.error("Error requesting online users count:", err);
      }

      try {
        await connection.invoke("RequestUnreadCount");
        console.log("Requested unread count");
      } catch (err) {
        console.error("Error requesting unread count:", err);
      }
    } catch (err) {
      console.error("Error connecting to ChatHub:", err);
      setTimeout(startConnection, 5000);
    }
  }

  /**
   * Send a message
   */
  async function sendMessage(recipientId, messageContent) {
    if (!connection || connection.state !== "Connected") {
      throw new Error("Not connected to chat server");
    }

    try {
      await connection.invoke("SendMessage", recipientId, messageContent);
    } catch (err) {
      console.error("Error sending message:", err);
      throw err;
    }
  }

  /**
   * Mark messages as read
   */
  async function markMessagesAsRead(senderId) {
    if (!connection || connection.state !== "Connected") {
      return;
    }

    try {
      await connection.invoke("MarkMessagesAsRead", senderId);
    } catch (err) {
      console.error("Error marking messages as read:", err);
    }
  }

  /**
   * Send typing indicator
   */
  async function sendTypingIndicator(recipientId) {
    if (!connection || connection.state !== "Connected") {
      return;
    }

    try {
      await connection.invoke("UserTyping", recipientId);
    } catch (err) {
      console.error("Error sending typing indicator:", err);
    }
  }

  /**
   * Send stopped typing indicator
   */
  async function sendStoppedTyping(recipientId) {
    if (!connection || connection.state !== "Connected") {
      return;
    }

    try {
      await connection.invoke("UserStoppedTyping", recipientId);
    } catch (err) {
      console.error("Error sending stopped typing indicator:", err);
    }
  }

  /**
   * Open a conversation
   */
  function openConversation(otherUserId) {
    currentConversationUserId = otherUserId;

    // Mark messages as read
    markMessagesAsRead(otherUserId);
  }

  /**
   * Handle new message
   */
  function handleNewMessage(message) {
    // Update unread count
    updateUnreadBadge(message.unreadCount);

    // If we're in the conversation view with this user, add the message
    if (
      currentConversationUserId &&
      (message.senderId === currentConversationUserId ||
        message.recipientId === currentConversationUserId)
    ) {
      addMessageToUI(message);

      // Mark as read if we're the recipient
      if (message.recipientId === userId) {
        markMessagesAsRead(message.senderId);
      }
    } else {
      // Show notification
      showMessageNotification(message);
    }

    // Play notification sound
    playNotificationSound();
  }

  /**
   * Add message to UI
   */
  function addMessageToUI(message) {
    const messagesContainer = document.getElementById("chatMessages");
    if (!messagesContainer) return;

    const isSentByCurrentUser = message.senderId === userId;
    const messageHtml = createMessageHTML(message, isSentByCurrentUser);

    messagesContainer.insertAdjacentHTML("beforeend", messageHtml);

    // Scroll to bottom
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
  }

  /**
   * Create message HTML
   */
  function createMessageHTML(message, isSentByCurrentUser) {
    const avatarHtml = message.senderAvatarUrl
      ? `<img src="${message.senderAvatarUrl}" alt="${message.senderUsername}" class="chat-avatar-small" />`
      : `<div class="chat-avatar-placeholder-small">${message.senderInitials}</div>`;

    const attachmentsHtml = message.attachments
      ? message.attachments
          .map((att) => {
            if (att.isImage) {
              return `<div class="attachment-image"><img src="${att.filePath}" alt="${att.originalFileName}" class="img-fluid rounded" /></div>`;
            } else {
              return `<div class="attachment-file"><a href="${att.filePath}" target="_blank" class="text-decoration-none"><i class="bi ${att.fileIcon}"></i> ${att.originalFileName} <small class="text-muted">(${att.fileSizeFormatted})</small></a></div>`;
            }
          })
          .join("")
      : "";

    return `
            <div class="message-wrapper ${
              isSentByCurrentUser ? "sent" : "received"
            }" data-message-id="${message.messageId}">
                ${
                  !isSentByCurrentUser
                    ? `<div class="message-avatar">${avatarHtml}</div>`
                    : ""
                }
                <div class="message-bubble ${
                  isSentByCurrentUser ? "bg-primary text-white" : "bg-light"
                }">
                    <div class="message-content">${message.messageContent}</div>
                    ${
                      attachmentsHtml
                        ? `<div class="message-attachments mt-2">${attachmentsHtml}</div>`
                        : ""
                    }
                    <div class="message-time">
                        <small>${message.timeAgo}</small>
                        ${
                          isSentByCurrentUser && message.isRead
                            ? '<i class="bi bi-check-all text-info" title="Read"></i>'
                            : isSentByCurrentUser
                            ? '<i class="bi bi-check" title="Sent"></i>'
                            : ""
                        }
                    </div>
                </div>
            </div>
        `;
  }

  /**
   * Mark messages as read in UI
   */
  function markMessagesAsReadInUI(readByUserId) {
    const messages = document.querySelectorAll(
      `.message-wrapper.sent[data-sender-id="${userId}"]`
    );
    messages.forEach((msg) => {
      const checkIcon = msg.querySelector(".bi-check");
      if (checkIcon) {
        checkIcon.classList.remove("bi-check");
        checkIcon.classList.add("bi-check-all", "text-info");
        checkIcon.title = "Read";
      }
    });
  }

  /**
   * Show typing indicator
   */
  function showTypingIndicator() {
    const indicator = document.getElementById("typingIndicator");
    const status = document.getElementById("userStatus");

    if (indicator && status) {
      status.style.display = "none";
      indicator.style.display = "inline";
    }
  }

  /**
   * Hide typing indicator
   */
  function hideTypingIndicator() {
    const indicator = document.getElementById("typingIndicator");
    const status = document.getElementById("userStatus");

    if (indicator && status) {
      indicator.style.display = "none";
      status.style.display = "inline";
    }
  }

  /**
   * Update user online status
   */
  function updateUserOnlineStatus(userId, isOnline) {
    // Update in conversation list
    const conversationItems = document.querySelectorAll(
      `.conversation-item[data-user-id="${userId}"]`
    );
    conversationItems.forEach((item) => {
      const indicator = item.querySelector(".online-indicator");
      if (indicator) {
        indicator.style.display = isOnline ? "block" : "none";
      }
    });

    // Update in conversation header
    if (currentConversationUserId === userId) {
      const headerIndicator = document.querySelector(
        ".card-header .online-indicator"
      );
      const statusText = document.getElementById("userStatus");

      if (headerIndicator) {
        headerIndicator.style.display = isOnline ? "block" : "none";
      }

      if (statusText) {
        statusText.textContent = isOnline ? "Online" : "Offline";
      }
    }
  }

  /**
   * Update online users count
   */
  function updateOnlineUsersCount(count) {
    const badge = document.getElementById("onlineUsersCount");
    if (badge) {
      badge.textContent = count;
      console.log("Updated online users count display to:", count);
    } else {
      console.warn("Online users count element not found");
    }
  }

  /**
   * Update unread messages badge
   */
  function updateUnreadBadge(count) {
    const badge = document.getElementById("chatUnreadBadge");
    if (badge) {
      if (count > 0) {
        badge.textContent = count > 99 ? "99+" : count;
        badge.style.display = "inline-block";
      } else {
        badge.style.display = "none";
      }
    }
  }

  /**
   * Show message notification
   */
  function showMessageNotification(message) {
    // Check if browser supports notifications
    if (!("Notification" in window)) {
      return;
    }

    // Check notification permission
    if (Notification.permission === "granted") {
      const notification = new Notification(
        `New message from ${message.senderFullName}`,
        {
          body: stripHtml(message.messageContent).substring(0, 100),
          icon: message.senderAvatarUrl || "/favicon.ico",
          tag: `chat-${message.messageId}`,
        }
      );

      notification.onclick = function () {
        window.focus();
        window.location.href = `/Chat/Conversation?userId=${message.senderId}`;
      };
    } else if (Notification.permission !== "denied") {
      Notification.requestPermission();
    }
  }

  /**
   * Play notification sound
   */
  function playNotificationSound() {
    // Simple beep sound using Web Audio API
    try {
      const audioContext = new (window.AudioContext ||
        window.webkitAudioContext)();
      const oscillator = audioContext.createOscillator();
      const gainNode = audioContext.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(audioContext.destination);

      oscillator.frequency.value = 800;
      oscillator.type = "sine";

      gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(
        0.01,
        audioContext.currentTime + 0.1
      );

      oscillator.start(audioContext.currentTime);
      oscillator.stop(audioContext.currentTime + 0.1);
    } catch (err) {
      console.log("Could not play notification sound:", err);
    }
  }

  /**
   * Show error message
   */
  function showError(message) {
    // You can customize this to use your preferred notification system
    alert(message);
  }

  /**
   * Strip HTML tags from string
   */
  function stripHtml(html) {
    const tmp = document.createElement("DIV");
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || "";
  }

  /**
   * Format file size
   */
  function formatFileSize(bytes) {
    const sizes = ["B", "KB", "MB", "GB"];
    if (bytes === 0) return "0 B";
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + " " + sizes[i];
  }

  /**
   * Validate file
   */
  function validateFile(file, type) {
    const maxSizeMB = type === "image" ? 5 : 10;
    const maxSizeBytes = maxSizeMB * 1024 * 1024;

    if (file.size > maxSizeBytes) {
      showError(`${file.name} is too large. Maximum size is ${maxSizeMB}MB.`);
      return false;
    }

    if (type === "image") {
      const validTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
      if (!validTypes.includes(file.type)) {
        showError(
          `${file.name} is not a valid image format. Allowed: JPG, PNG, GIF, WebP`
        );
        return false;
      }
    } else {
      const validExtensions = [".pdf", ".doc", ".docx", ".zip", ".rar"];
      const fileName = file.name.toLowerCase();
      const isValid = validExtensions.some((ext) => fileName.endsWith(ext));
      if (!isValid) {
        showError(
          `${file.name} is not a valid file format. Allowed: PDF, DOC, DOCX, ZIP, RAR`
        );
        return false;
      }
    }

    return true;
  }

  // Public API
  return {
    init: init,
    sendMessage: sendMessage,
    markMessagesAsRead: markMessagesAsRead,
    sendTypingIndicator: sendTypingIndicator,
    sendStoppedTyping: sendStoppedTyping,
    openConversation: openConversation,
    validateFile: validateFile,
    formatFileSize: formatFileSize,
    connection: connection,
  };
})();
