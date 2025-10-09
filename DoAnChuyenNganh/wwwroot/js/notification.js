/**
 * Notification Manager - Handles real-time notifications using SignalR
 */
const NotificationManager = (function () {
    let connection = null;
    let userId = null;
    let isInitialized = false;

    /**
     * Initialize the notification system
     */
    function init(currentUserId) {
        if (isInitialized) {
            console.warn('NotificationManager already initialized');
            return;
        }

        userId = currentUserId;
        setupSignalR();
        loadUnreadCount();
        setupEventHandlers();
        isInitialized = true;

        console.log('NotificationManager initialized for user:', userId);
    }

    /**
     * Setup SignalR connection
     */
    function setupSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        // Handle incoming notifications
        connection.on("ReceiveNotification", function (notification) {
            console.log('Received notification:', notification);
            handleNewNotification(notification);
        });

        // Handle unread count updates
        connection.on("UpdateUnreadCount", function (count) {
            console.log('Unread count updated:', count);
            updateUnreadBadge(count);
        });

        // Start connection
        connection.start()
            .then(function () {
                console.log('SignalR connected');
            })
            .catch(function (err) {
                console.error('SignalR connection error:', err);
                // Retry connection after 5 seconds
                setTimeout(() => setupSignalR(), 5000);
            });

        // Handle reconnection
        connection.onreconnected(function () {
            console.log('SignalR reconnected');
            loadUnreadCount();
        });
    }

    /**
     * Handle new notification received
     */
    function handleNewNotification(notification) {
        // Update unread count
        loadUnreadCount();

        // Show toast notification
        showToast(notification);

        // Play notification sound (optional)
        playNotificationSound();

        // Reload dropdown if it's open
        const dropdown = document.getElementById('notificationDropdown');
        if (dropdown && dropdown.classList.contains('show')) {
            loadNotificationDropdown();
        }
    }

    /**
     * Show toast notification
     */
    function showToast(notification) {
        // Create toast element
        const toastHtml = `
            <div class="toast notification-toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="5000">
                <div class="toast-header">
                    <i class="${notification.icon} ${notification.iconColor} me-2"></i>
                    <strong class="me-auto">${notification.title}</strong>
                    <small>${notification.timeAgo}</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${notification.message}
                    ${notification.url ? `<a href="${notification.url}" class="btn btn-sm btn-primary mt-2">View</a>` : ''}
                </div>
            </div>
        `;

        // Add to toast container
        let toastContainer = document.getElementById('notificationToastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'notificationToastContainer';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const toastElement = $(toastHtml);
        $(toastContainer).append(toastElement);

        // Show toast
        const toast = new bootstrap.Toast(toastElement[0]);
        toast.show();

        // Remove from DOM after hidden
        toastElement.on('hidden.bs.toast', function () {
            $(this).remove();
        });
    }

    /**
     * Play notification sound
     */
    function playNotificationSound() {
        // Optional: Add notification sound
        // const audio = new Audio('/sounds/notification.mp3');
        // audio.play().catch(err => console.log('Could not play sound:', err));
    }

    /**
     * Load unread count
     */
    function loadUnreadCount() {
        $.ajax({
            url: '/Notification/GetUnreadCount',
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    updateUnreadBadge(response.count);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading unread count:', error);
            }
        });
    }

    /**
     * Update unread badge
     */
    function updateUnreadBadge(count) {
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    /**
     * Load notification dropdown
     */
    function loadNotificationDropdown() {
        const dropdownMenu = document.getElementById('notificationDropdownMenu');
        if (!dropdownMenu) return;

        // Show loading
        dropdownMenu.innerHTML = `
            <div class="text-center py-3">
                <div class="spinner-border spinner-border-sm" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `;

        $.ajax({
            url: '/Notification/GetRecent',
            type: 'GET',
            data: { count: 10 },
            success: function (response) {
                if (response.success) {
                    renderNotificationDropdown(response.notifications);
                } else {
                    dropdownMenu.innerHTML = '<div class="text-center py-3 text-danger">Error loading notifications</div>';
                }
            },
            error: function () {
                dropdownMenu.innerHTML = '<div class="text-center py-3 text-danger">Error loading notifications</div>';
            }
        });
    }

    /**
     * Render notification dropdown
     */
    function renderNotificationDropdown(notifications) {
        const dropdownMenu = document.getElementById('notificationDropdownMenu');
        if (!dropdownMenu) return;

        let html = `
            <div class="notification-dropdown-header">
                <h6 class="mb-0">Notifications</h6>
                ${notifications.some(n => !n.isRead) ? '<button type="button" class="btn btn-sm btn-link mark-all-read-btn">Mark all as read</button>' : ''}
            </div>
            <div class="notification-dropdown-body">
        `;

        if (notifications.length > 0) {
            notifications.forEach(notification => {
                html += `
                    <div class="notification-dropdown-item ${notification.isRead ? 'read' : 'unread'}" 
                         data-notification-id="${notification.notificationId}">
                        <div class="d-flex align-items-start">
                            <div class="notification-icon me-2">
                                <i class="${notification.icon} ${notification.iconColor}"></i>
                            </div>
                            <div class="flex-grow-1">
                                <div class="notification-title">${notification.title}</div>
                                <div class="notification-message">${notification.message}</div>
                                <div class="notification-time">${notification.timeAgo}</div>
                            </div>
                            ${!notification.isRead ? '<div class="unread-indicator"></div>' : ''}
                        </div>
                        ${notification.url ? `<a href="${notification.url}" class="stretched-link notification-link"></a>` : ''}
                    </div>
                `;
            });
        } else {
            html += `
                <div class="notification-empty">
                    <i class="bi bi-bell-slash"></i>
                    <p>No notifications</p>
                </div>
            `;
        }

        html += `
            </div>
            <div class="notification-dropdown-footer">
                <a href="/Notification/Index" class="btn btn-sm btn-link w-100">View all notifications</a>
            </div>
        `;

        dropdownMenu.innerHTML = html;
    }

    /**
     * Setup event handlers
     */
    function setupEventHandlers() {
        // Load dropdown when opened
        $(document).on('show.bs.dropdown', '#notificationDropdown', function () {
            loadNotificationDropdown();
        });

        // Mark as read when clicking notification
        $(document).on('click', '.notification-dropdown-item', function (e) {
            if ($(e.target).closest('.mark-as-read-btn, .delete-notification-btn').length) {
                return; // Don't mark as read if clicking action buttons
            }

            const notificationId = $(this).data('notification-id');
            const isRead = $(this).hasClass('read');

            if (!isRead) {
                markAsRead(notificationId);
            }
        });

        // Mark all as read
        $(document).on('click', '.mark-all-read-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            markAllAsRead();
        });

        // Mark single as read
        $(document).on('click', '.mark-as-read-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const notificationId = $(this).data('notification-id');
            markAsRead(notificationId);
        });

        // Delete notification
        $(document).on('click', '.delete-notification-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const notificationId = $(this).data('notification-id');
            deleteNotification(notificationId);
        });
    }

    /**
     * Mark notification as read
     */
    function markAsRead(notificationId) {
        $.ajax({
            url: '/Notification/MarkAsRead/' + notificationId,
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    // Update UI
                    $(`.notification-item[data-notification-id="${notificationId}"]`).addClass('read').removeClass('unread');
                    $(`.notification-dropdown-item[data-notification-id="${notificationId}"]`).addClass('read').removeClass('unread');
                    loadUnreadCount();
                }
            },
            error: function () {
                console.error('Error marking notification as read');
            }
        });
    }

    /**
     * Mark all notifications as read
     */
    function markAllAsRead() {
        $.ajax({
            url: '/Notification/MarkAllAsRead',
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    loadNotificationDropdown();
                    loadUnreadCount();
                }
            },
            error: function () {
                console.error('Error marking all notifications as read');
            }
        });
    }

    /**
     * Delete notification
     */
    function deleteNotification(notificationId) {
        $.ajax({
            url: '/Notification/Delete/' + notificationId,
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    // Remove from UI
                    $(`.notification-item[data-notification-id="${notificationId}"]`).fadeOut(300, function () {
                        $(this).remove();
                    });
                    loadNotificationDropdown();
                    loadUnreadCount();
                }
            },
            error: function () {
                console.error('Error deleting notification');
            }
        });
    }

    // Public API
    return {
        init: init,
        loadUnreadCount: loadUnreadCount,
        loadNotificationDropdown: loadNotificationDropdown
    };
})();

