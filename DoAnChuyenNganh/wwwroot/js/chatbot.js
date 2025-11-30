const ChatbotManager = (function () {
  // Configuration
  const API_BASE_URL = "http://localhost:8000"; // Chatbot API base URL
  const MAX_MESSAGE_LENGTH = 5000;
  const MIN_MESSAGE_INTERVAL = 2000; // 2 seconds between messages
  const RATE_LIMIT_COOLDOWN = 3000; // 3 seconds cooldown after rate limit

  // State
  let currentUserId = null;
  let userAvatarUrl = null;
  let currentConversationId = null;
  let conversations = [];
  let lastMessageTime = 0;
  let isProcessing = false;

  // DOM Elements
  let elements = {};

  // Initialize
  function init(userId, avatarUrl) {
    currentUserId = userId ? userId.toString() : null;
    userAvatarUrl = avatarUrl || null;

    if (!currentUserId) {
      console.error("User ID is required");
      window.location.href = "/Account/Login";
      return;
    }

    cacheElements();
    attachEventListeners();
    loadConversations();
  }

  // Cache DOM elements
  function cacheElements() {
    elements = {
      conversationList: document.getElementById("conversationList"),
      chatMessages: document.getElementById("chatMessages"),
      messageInput: document.getElementById("messageInput"),
      messageForm: document.getElementById("messageForm"),
      sendBtn: document.getElementById("sendBtn"),
      charCount: document.getElementById("charCount"),
      newConversationBtn: document.getElementById("newConversationBtn"),
      startNewChatBtn: document.getElementById("startNewChatBtn"),
      deleteConversationBtn: document.getElementById("deleteConversationBtn"),
      conversationTitle: document.getElementById("conversationTitle"),
      conversationSubtitle: document.getElementById("conversationSubtitle"),
      welcomeMessage: document.getElementById("welcomeMessage"),
      generateSqlCheck: document.getElementById("generateSqlCheck"),
      rateLimitWarning: document.getElementById("rateLimitWarning"),
      rateLimitMessage: document.getElementById("rateLimitMessage"),
      errorAlert: document.getElementById("errorAlert"),
      errorMessage: document.getElementById("errorMessage"),
      conversationSearch: document.getElementById("conversationSearch"),
      toggleSidebarBtn: document.getElementById("toggleSidebarBtn"),
      chatbotSidebar: document.getElementById("chatbotSidebar"),
    };
  }

  // Attach event listeners
  function attachEventListeners() {
    // Message form submission
    elements.messageForm.addEventListener("submit", handleSendMessage);

    // Character count
    elements.messageInput.addEventListener("input", updateCharacterCount);

    // Enable/disable send button
    elements.messageInput.addEventListener("input", toggleSendButton);

    // New conversation buttons
    elements.newConversationBtn.addEventListener(
      "click",
      createNewConversation
    );
    if (elements.startNewChatBtn) {
      elements.startNewChatBtn.addEventListener("click", createNewConversation);
    }

    // Delete conversation
    elements.deleteConversationBtn.addEventListener(
      "click",
      deleteCurrentConversation
    );

    // Search conversations
    elements.conversationSearch.addEventListener("input", filterConversations);

    // Toggle sidebar on mobile
    if (elements.toggleSidebarBtn) {
      elements.toggleSidebarBtn.addEventListener("click", toggleSidebar);
    }

    // Enter to send (Shift+Enter for new line)
    elements.messageInput.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        elements.messageForm.dispatchEvent(new Event("submit"));
      }
    });
  }

  // Update character count
  function updateCharacterCount() {
    const length = elements.messageInput.value.length;
    elements.charCount.textContent = length;

    const charCountContainer = elements.charCount.parentElement;
      if (length > MAX_MESSAGE_LENGTH * 0.9) {
          charCountContainer.classList.add("text-danger");
      } else {
          charCountContainer.classList.remove("text-danger");
      }
  }

  // Toggle send button
  function toggleSendButton() {
    const message = elements.messageInput.value.trim();
    elements.sendBtn.disabled =
      message.length === 0 ||
      message.length > MAX_MESSAGE_LENGTH ||
      isProcessing;
  }

  // Load conversations from API
  async function loadConversations() {
    try {
      showLoadingInSidebar();

      const response = await fetch(
        `${API_BASE_URL}/api/conversations?user_id=${currentUserId}&limit=50`
      );

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      conversations = data.conversations || [];

      renderConversations();
    } catch (error) {
      console.error("Error loading conversations:", error);
      showErrorInSidebar(
        "Failed to load conversations. Please refresh the page."
      );
    }
  }

  // Render conversations in sidebar
  function renderConversations() {
    if (conversations.length === 0) {
      elements.conversationList.innerHTML = `
                <div class="text-center py-5 text-muted">
                    <p class="small mb-2">No conversations yet</p>
                    <button class="btn btn-sm btn-outline-primary" onclick="ChatbotManager.createNewConversation()">
                        <i class="bi bi-plus"></i> New Chat
                    </button>
                </div>
            `;
      return;
    }

    let html = "";
    conversations.forEach((conv) => {
      const isActive = conv.conversation_id === currentConversationId;
      const title = conv.title || "New Conversation";
      const titlePreview =
        title.length > 30 ? title.substring(0, 30) + "..." : title;
      const timeAgo = formatTimeAgo(parseDateFromMongo(conv.updated_at));

        html += `
                <div class="conversation-item ${isActive ? "active" : ""}"
                     data-conversation-id="${conv.conversation_id}"
                     onclick="ChatbotManager.loadConversation('${conv.conversation_id
            }')">
                    <div class="conversation-title" title="${escapeHtml(title)}">${escapeHtml(
                titlePreview
            )}</div>
                    <div class="d-flex justify-content-between align-items-center mt-1">
                        <span class="conversation-date">${timeAgo}</span>
                        <span class="badge bg-light text-secondary border rounded-pill" style="font-size: 0.65rem;">
                            ${conv.message_count} msgs
                        </span>
                    </div>
                </div>
            `;
    });

    elements.conversationList.innerHTML = html;
  }

  // Load a specific conversation
  async function loadConversation(conversationId) {
    try {
      currentConversationId = conversationId;

      // Update UI
      renderConversations();
      elements.welcomeMessage.style.display = "none";
      elements.deleteConversationBtn.style.display = "inline-block";

      // Show loading
      elements.chatMessages.innerHTML = `
                <div class="text-center py-5">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-2 text-muted">Loading conversation...</p>
                </div>
            `;

      // Load conversation details
      const [convResponse, messagesResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/api/conversations/${conversationId}`),
        fetch(`${API_BASE_URL}/api/conversations/${conversationId}/messages`),
      ]);

      if (!convResponse.ok || !messagesResponse.ok) {
        throw new Error("Failed to load conversation");
      }

      const conversation = await convResponse.json();
      const messagesData = await messagesResponse.json();

      // Update header
      elements.conversationTitle.textContent =
        conversation.title || "AI Assistant";
      elements.conversationSubtitle.textContent = `${messagesData.total} messages`;

      // Render messages
      renderMessages(messagesData.messages || []);

      // Scroll to bottom
      scrollToBottom();
    } catch (error) {
      console.error("Error loading conversation:", error);
      showError("Failed to load conversation. Please try again.");
      elements.chatMessages.innerHTML = `
                <div class="text-center py-5 text-danger">
                    <i class="bi bi-exclamation-triangle fs-1"></i>
                    <p class="mt-2">Failed to load conversation</p>
                </div>
            `;
    }
  }

  // Render messages
  function renderMessages(messages) {
    if (messages.length === 0) {
      elements.chatMessages.innerHTML = `
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-chat-dots fs-1 opacity-50"></i>
                    <p class="mt-3">No messages yet. Start the conversation!</p>
                </div>
            `;
      return;
    }

    let html = "";
    messages.forEach((msg) => {
      html += createMessageHTML(msg);
    });

    elements.chatMessages.innerHTML = html;
  }

  // Create HTML for a single message
  function createMessageHTML(message) {
    const isUser = message.role === "user";
      const timeAgo = formatTimeAgo(parseDateFromMongo(message.created_at));
      const roleClass = isUser ? "user" : "ai";

    // Determine avatar HTML
    let avatarHTML = "";
    if (isUser) {
      // User avatar - use actual user avatar or fallback to icon
      if (userAvatarUrl) {
        avatarHTML = `<img src="${userAvatarUrl}" alt="User" />`;
      } else {
        avatarHTML = `<i class="bi bi-person text-secondary" style="font-size: 20px;"></i>`;
      }
    } else {
        avatarHTML = `<img src="/AI/ai_icon.jpg" alt="AI" onerror="this.src='https://cdn-icons-png.flaticon.com/512/4712/4712109.png'" />`;
    }

    let html = `
           <div class="message-wrapper ${roleClass}" data-message-id="${message.message_id}">
                <div class="message-avatar d-flex align-items-center justify-content-center">
                    ${avatarHTML}
                </div>
                <div class="message-bubble">
                    <div class="message-content">
                        ${formatMessageContent(message.content)}
                    </div>
                    <div class="message-time">${timeAgo}</div>
                </div>
            </div>
        `;

    return html;
  }

  // Parse MongoDB date format
  function parseDateFromMongo(dateValue) {
      if (!dateValue)
          return new Date();

    let parsedDate;

    if (typeof dateValue === "object" && dateValue !== null && dateValue.$date) {
      parsedDate = new Date(dateValue.$date);
    } else if (typeof dateValue === "string") {
        if (!dateValue.endsWith("Z") && !dateValue.includes("+") && !dateValue.includes("GMT")) {
            parsedDate = new Date(dateValue + "Z");
        } else {
            parsedDate = new Date(dateValue);
        }
    } else if (dateValue instanceof Date) {
      parsedDate = dateValue;
    }
    else {
      parsedDate = new Date(dateValue);
    }

    const now = new Date();
    if (parsedDate > now) {
        const yearDiff = parsedDate.getFullYear() - now.getFullYear();
        if (yearDiff === 1) parsedDate.setFullYear(parsedDate.getFullYear() - 1);
    }

    return parsedDate;
  }

  // Handle send message
  async function handleSendMessage(e) {
    e.preventDefault();

    const message = elements.messageInput.value.trim();

    // Validation
    if (!message) {
      return;
    }

    if (message.length > MAX_MESSAGE_LENGTH) {
      showError(
        `Message is too long. Maximum ${MAX_MESSAGE_LENGTH} characters allowed.`
      );
      return;
    }

    // Rate limiting
    const now = Date.now();
    if (now - lastMessageTime < MIN_MESSAGE_INTERVAL) {
      const waitTime = Math.ceil(
        (MIN_MESSAGE_INTERVAL - (now - lastMessageTime)) / 1000
      );
      showRateLimitWarning(
        `Please wait ${waitTime} second(s) before sending another message.`
      );
      return;
    }

    if (isProcessing) {
      showRateLimitWarning(
        "Please wait for the previous message to be processed."
      );
      return;
    }

    try {
      isProcessing = true;
      lastMessageTime = now;

      // Disable input
      elements.messageInput.disabled = true;
      elements.sendBtn.disabled = true;

      // Store the first message for title generation
      const isFirstMessage = !currentConversationId;
      const firstMessage = isFirstMessage ? message : null;

      // If this is the first message, create conversation with title first
      if (isFirstMessage) {
        const generatedTitle = generateConversationTitle(firstMessage);
        const newConversationId = await createConversationWithTitle(
          generatedTitle
        );

        if (newConversationId) {
          currentConversationId = newConversationId;
          console.log(
            "Created new conversation with title:",
            generatedTitle,
            "ID:",
            newConversationId
          );
        } else {
          console.warn(
            "Failed to create conversation with title, will let API create it"
          );
        }
      }

      // Add user message to UI immediately
      const userMessage = {
        message_id: "temp-" + Date.now(),
        conversation_id: currentConversationId || "new",
        role: "user",
        content: message,
        created_at: new Date().toISOString(),
      };

      if (currentConversationId) {
        appendMessage(userMessage);
      }

      // Clear input
      elements.messageInput.value = "";
      updateCharacterCount();

      // Show typing indicator
      showTypingIndicator();

      const response = await fetch(`${API_BASE_URL}/api/chat`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          conversation_id: currentConversationId,
          message: message,
          user_id: currentUserId,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();

      removeTypingIndicator();

      // If new conversation, update UI
      if (isFirstMessage) {
        currentConversationId = data.conversation_id;

        if (elements.welcomeMessage) {
          elements.welcomeMessage.style.display = "none";
        }

        elements.deleteConversationBtn.style.display = "inline-block";

        // Reload conversations to show the new one
        await loadConversations();

        // Add user message now if not already added
        if (
          !userMessage.conversation_id ||
          userMessage.conversation_id === "new"
        ) {
          appendMessage(userMessage);
        }
      }

      // Add assistant message
      const assistantMessage = {
        message_id: data.message_id,
        conversation_id: data.conversation_id,
        role: "assistant",
        content: data.assistant_message,
        created_at: data.timestamp,
      };

      appendMessage(assistantMessage);

      // Update conversation title display
      await updateConversationTitle(data.conversation_id);

      // Scroll to bottom
      scrollToBottom();
    } catch (error) {
      console.error("Error sending message:", error);
      removeTypingIndicator();
      showError("Failed to send message. Please try again.");
    } finally {
      isProcessing = false;
      elements.messageInput.disabled = false;
      elements.sendBtn.disabled = false;
      toggleSendButton();
      elements.messageInput.focus();
    }
  }

  // Generate conversation title from first message (first 11 words)
  function generateConversationTitle(message) {
    if (!message) {
      console.log("generateConversationTitle: No message provided");
      return "New Conversation";
    }

    const words = message.trim().split(/\s+/);
    const titleWords = words.slice(0, 11);
    let title = titleWords.join(" ");

    // Add ellipsis if there are more words
    if (words.length > 11) {
      title += "...";
    }

    console.log("generateConversationTitle: Generated title", {
      originalMessage: message,
      wordCount: words.length,
      generatedTitle: title,
    });

    return title || "New Conversation";
  }

  // Create a new conversation with title on server
  async function createConversationWithTitle(title) {
    try {
      console.log("createConversationWithTitle: Creating conversation", {
        title,
        userId: currentUserId,
      });

      const response = await fetch(`${API_BASE_URL}/api/conversations`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: title,
          user_id: currentUserId,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error("Failed to create conversation", {
          status: response.status,
          statusText: response.statusText,
          error: errorText,
        });
        return null;
      }

      const data = await response.json();
      console.log(
        "createConversationWithTitle: Conversation created successfully",
        data
      );
      return data.conversation_id;
    } catch (error) {
      console.error("Error creating conversation:", error);
      return null;
    }
  }

  // Append a message to the chat
  function appendMessage(message) {
    const messageHTML = createMessageHTML(message);
    elements.chatMessages.insertAdjacentHTML("beforeend", messageHTML);
  }

  // Show typing indicator
  function showTypingIndicator() {
    const html = `
            <div class="message-wrapper ai typing-indicator-group">
                <div class="message-avatar d-flex align-items-center justify-content-center">
                    <img src="/AI/ai_icon.jpg" alt="AI" />
                </div>
                <div class="message-bubble">
                    <div class="message-content p-2">
                        <div class="typing-indicator">
                            <span></span>
                            <span></span>
                            <span></span>

                        </div>
                    </div>
                </div>
            </div>
        `;
    elements.chatMessages.insertAdjacentHTML("beforeend", html);
    scrollToBottom();
  }

  // Remove typing indicator
  function removeTypingIndicator() {
    const indicator = elements.chatMessages.querySelector(
      ".typing-indicator-group"
    );
    if (indicator) {
      indicator.remove();
    }
  }

  function createNewConversation() {
    console.log("createNewConversation: Preparing UI for new conversation");

    currentConversationId = null;

    const conversationItems = document.querySelectorAll(".conversation-item");
    conversationItems.forEach((item) => item.classList.remove("active"));

    elements.chatMessages.innerHTML = "";

    // Show welcome message if it exists, otherwise create a simple placeholder
    if (elements.welcomeMessage) {
      elements.welcomeMessage.style.display = "flex";
    } else {
      // Fallback: create a simple welcome message
      elements.chatMessages.innerHTML = `
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-chat-dots" style="font-size: 3rem;"></i>
                    <p class="mt-3 mb-2 fs-5">Start a New Conversation</p>
                    <p class="small">Type your message below to begin chatting with the AI assistant.</p>
                </div>
            `;
    }

    // Update header
    elements.conversationTitle.textContent = "New Conversation";
    elements.conversationSubtitle.textContent = "No messages yet";

    // Hide delete button (no conversation to delete yet)
    elements.deleteConversationBtn.style.display = "none";

    // Focus on input
    elements.messageInput.focus();

    console.log("createNewConversation: UI ready for new conversation");
  }

  // Delete current conversation
  async function deleteCurrentConversation() {
    if (!currentConversationId) return;

    if (
      !confirm(
        "Are you sure you want to delete this conversation? This action cannot be undone."
      )
    ) {
      return;
    }

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/conversations/${currentConversationId}`,
        {
          method: "DELETE",
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // Reset state
      currentConversationId = null;

      // Show welcome message if it exists
      if (elements.welcomeMessage) {
        elements.welcomeMessage.style.display = "flex";
      }

      elements.deleteConversationBtn.style.display = "none";
      elements.chatMessages.innerHTML = "";
      elements.conversationTitle.textContent = "AI Assistant";
      elements.conversationSubtitle.textContent =
        "Ask me anything about your data";

      // Reload conversations
      await loadConversations();
    } catch (error) {
      console.error("Error deleting conversation:", error);
      showError("Failed to delete conversation. Please try again.");
    }
  }

  // Update conversation title based on first message
  async function updateConversationTitle(conversationId) {
    try {
      // Get conversation details
      const response = await fetch(
        `${API_BASE_URL}/api/conversations/${conversationId}`
      );
      if (!response.ok) return;

      const conversation = await response.json();

      // Update in conversations list
      const index = conversations.findIndex(
        (c) => c.conversation_id === conversationId
      );
      if (index !== -1) {
        conversations[index] = conversation;
        renderConversations();
      } else {
        // Add new conversation to list
        conversations.unshift(conversation);
        renderConversations();
      }

      // Update header if this is the active conversation
      if (conversationId === currentConversationId) {
        elements.conversationTitle.textContent =
          conversation.title || "AI Assistant";
        elements.conversationSubtitle.textContent = `${conversation.message_count} messages`;
      }
    } catch (error) {
      console.error("Error updating conversation title:", error);
    }
  }

// Filter conversations
    function filterConversations() {
        const searchTerm = elements.conversationSearch.value.toLowerCase().trim();
        const items = elements.conversationList.querySelectorAll(".conversation-item");
        items.forEach((item) => {
            const titleElement = item.querySelector(".conversation-title");

            if (titleElement) {
                const title = titleElement.textContent.toLowerCase();
                if (title.includes(searchTerm)) {
                    item.style.display = "block";
                } else {
                    item.style.display = "none";
                }
            }
        });
    }

  // Toggle sidebar on mobile
  function toggleSidebar() {
    elements.chatbotSidebar.classList.toggle("show");
  }

  // Scroll to bottom of messages
  function scrollToBottom() {
    setTimeout(() => {
      elements.chatMessages.scrollTop = elements.chatMessages.scrollHeight;
    }, 100);
  }

  // Show loading in sidebar
  function showLoadingInSidebar() {
    elements.conversationList.innerHTML = `
            <div class="text-center text-muted py-4">
                <div class="spinner-border spinner-border-sm" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2 small">Loading conversations...</p>
            </div>
        `;
  }

  // Show error in sidebar
  function showErrorInSidebar(message) {
    elements.conversationList.innerHTML = `
            <div class="text-center text-danger py-4">
                <i class="bi bi-exclamation-triangle"></i>
                <p class="mt-2 small">${escapeHtml(message)}</p>
                <button class="btn btn-sm btn-primary mt-2" onclick="ChatbotManager.loadConversations()">
                    <i class="bi bi-arrow-clockwise"></i> Retry
                </button>
            </div>
        `;
  }

  // Show error alert
  function showError(message) {
    elements.errorMessage.textContent = message;
    elements.errorAlert.style.display = "block";

    setTimeout(() => {
      elements.errorAlert.style.display = "none";
    }, 5000);
  }

  // Show rate limit warning
  function showRateLimitWarning(message) {
    elements.rateLimitMessage.textContent = message;
    elements.rateLimitWarning.style.display = "block";

    setTimeout(() => {
      elements.rateLimitWarning.style.display = "none";
    }, RATE_LIMIT_COOLDOWN);
  }

  function formatMessageContent(content) {
    // Escape HTML
    let formatted = escapeHtml(content);

    // Convert line breaks to <br>
    formatted = formatted.replace(/\n/g, "<br>");

    // Convert URLs to links
    formatted = formatted.replace(
      /(https?:\/\/[^\s]+)/g,
      '<a href="$1" target="_blank" rel="noopener noreferrer">$1</a>'
    );

    return formatted;
  }

  // Escape HTML
  function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  // Format time ago
  function formatTimeAgo(date) {
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
  }

  // Public API
  return {
    init: init,
    loadConversation: loadConversation,
    createNewConversation: createNewConversation,
    loadConversations: loadConversations,
  };
})();

// Make it globally available
window.ChatbotManager = ChatbotManager;
