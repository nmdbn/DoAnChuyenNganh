// Forum JavaScript Functions

// Get CSRF token
function getAntiForgeryToken() {
  return document.querySelector('input[name="__RequestVerificationToken"]')
    .value;
}

// Like Post
async function likePost(postId) {
  try {
    const response = await fetch(`/Forum/LikePost/${postId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: getAntiForgeryToken(),
      },
    });

    const result = await response.json();

    if (result.success) {
      // Update like count
      const likeCountElement = document.getElementById("like-count");
      if (likeCountElement) {
        likeCountElement.textContent = result.newCount;
      }

      // Update button style
      const likeButton = document.getElementById("like-post-btn");
      if (likeButton) {
        if (result.isLiked) {
          likeButton.classList.remove("btn-outline-danger");
          likeButton.classList.add("btn-danger");
        } else {
          likeButton.classList.remove("btn-danger");
          likeButton.classList.add("btn-outline-danger");
        }
      }
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error liking post:", error);
    alert("An error occurred. Please try again.");
  }
}

// Like Reply
async function likeReply(replyId) {
  try {
    const response = await fetch(`/Forum/LikeReply/${replyId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: getAntiForgeryToken(),
      },
    });

    const result = await response.json();

    if (result.success) {
      // Update like count
      const likeCountElement = document.getElementById(
        `like-reply-${replyId}-count`
      );
      if (likeCountElement) {
        likeCountElement.textContent = result.newCount;
      }

      // Update button style
      const likeButton = document.getElementById(`like-reply-${replyId}-btn`);
      if (likeButton) {
        if (result.isLiked) {
          likeButton.classList.remove("btn-outline-danger");
          likeButton.classList.add("btn-danger");
        } else {
          likeButton.classList.remove("btn-danger");
          likeButton.classList.add("btn-outline-danger");
        }
      }
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error liking reply:", error);
    alert("An error occurred. Please try again.");
  }
}

// Bookmark Post
async function bookmarkPost(postId, title) {
  try {
    const response = await fetch(
      `/Forum/BookmarkPost/${postId}?title=${encodeURIComponent(title)}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: getAntiForgeryToken(),
        },
      }
    );

    const result = await response.json();

    if (result.success) {
      // Update button
      const bookmarkButton = document.getElementById("bookmark-post-btn");
      if (bookmarkButton) {
        if (result.isBookmarked) {
          bookmarkButton.classList.remove("btn-outline-warning");
          bookmarkButton.classList.add("btn-warning");
          bookmarkButton.innerHTML =
            '<i class="bi bi-bookmark-fill"></i> Bookmarked';
        } else {
          bookmarkButton.classList.remove("btn-warning");
          bookmarkButton.classList.add("btn-outline-warning");
          bookmarkButton.innerHTML =
            '<i class="bi bi-bookmark-fill"></i> Bookmark';
        }
      }
      alert(result.message);
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error bookmarking post:", error);
    alert("An error occurred. Please try again.");
  }
}

// Delete Post
async function deletePost(postId) {
  if (
    !confirm(
      "Are you sure you want to delete this post? This action cannot be undone."
    )
  ) {
    return;
  }

  try {
    const formData = new FormData();
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

    const response = await fetch(`/Forum/DeletePost/${postId}`, {
      method: "POST",
      body: formData,
    });

    const result = await response.json();

    if (result.success) {
      alert(result.message);
      if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
      }
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error deleting post:", error);
    alert("An error occurred. Please try again.");
  }
}

// Delete Reply
async function deleteReply(replyId) {
  if (
    !confirm(
      "Are you sure you want to delete this reply? This action cannot be undone."
    )
  ) {
    return;
  }

  try {
    const formData = new FormData();
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

    const response = await fetch(`/Forum/DeleteReply/${replyId}`, {
      method: "POST",
      body: formData,
    });

    const result = await response.json();

    if (result.success) {
      alert(result.message);
      location.reload();
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error deleting reply:", error);
    alert("An error occurred. Please try again.");
  }
}

// Edit Reply
function editReply(replyId) {
  const replyBody = document.getElementById(`reply-body-${replyId}`);
  const editForm = document.getElementById(`reply-edit-form-${replyId}`);

  if (replyBody && editForm) {
    replyBody.style.display = "none";
    editForm.style.display = "block";

    // Initialize TinyMCE for edit reply textarea if not already initialized
    const textareaId = `reply-edit-content-${replyId}`;
    if (
      typeof tinymce !== "undefined" &&
      !tinymce.get(textareaId) &&
      document.getElementById(textareaId)
    ) {
      tinymce.init({
        selector: `#${textareaId}`,
        height: 250,
        menubar: false,
        license_key: 'gpl',
        plugins: [
          "advlist",
          "autolink",
          "lists",
          "link",
          "charmap",
          "searchreplace",
          "visualblocks",
          "code",
          "insertdatetime",
          "table",
          "help",
          "wordcount",
        ],
        toolbar:
          "undo redo | blocks | " +
          "bold italic underline | bullist numlist | " +
          "link code | removeformat",
        content_style:
          'body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif; font-size: 14px }',
        branding: false,
        promotion: false,
      });
    }
  }
}

// Cancel Reply Edit
function cancelReplyEdit(replyId) {
  const replyBody = document.getElementById(`reply-body-${replyId}`);
  const editForm = document.getElementById(`reply-edit-form-${replyId}`);

  if (replyBody && editForm) {
    replyBody.style.display = "block";
    editForm.style.display = "none";

    // Remove TinyMCE instance if exists
    const textareaId = `reply-edit-content-${replyId}`;
    if (typeof tinymce !== "undefined" && tinymce.get(textareaId)) {
      tinymce.get(textareaId).remove();
    }
  }
}

// Save Reply Edit
async function saveReplyEdit(replyId) {
  const textareaId = `reply-edit-content-${replyId}`;
  const contentTextarea = document.getElementById(textareaId);
  if (!contentTextarea) return;

  // Get content from TinyMCE if available
  let content = "";
  if (typeof tinymce !== "undefined" && tinymce.get(textareaId)) {
    content = tinymce.get(textareaId).getContent();
  } else {
    content = contentTextarea.value;
  }

  try {
    const formData = new FormData();
    formData.append("ReplyId", replyId);
    formData.append("Content", content);
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

    // Add uploaded images
    const imageInput = document.getElementById(`reply-edit-images-${replyId}`);
    if (imageInput && imageInput.files.length > 0) {
      Array.from(imageInput.files).forEach((file) => {
        formData.append("UploadedImages", file);
      });
    }

    // Add uploaded files
    const fileInput = document.getElementById(`reply-edit-files-${replyId}`);
    if (fileInput && fileInput.files.length > 0) {
      Array.from(fileInput.files).forEach((file) => {
        formData.append("UploadedFiles", file);
      });
    }

    const response = await fetch("/Forum/EditReply", {
      method: "POST",
      body: formData,
    });

    const result = await response.json();

    if (result.success) {
      alert(result.message);
      location.reload();
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error editing reply:", error);
    alert("An error occurred. Please try again.");
  }
}

// Toggle Sticky
async function toggleSticky(postId) {
  try {
    const formData = new FormData();
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

    const response = await fetch(`/Forum/ToggleSticky/${postId}`, {
      method: "POST",
      body: formData,
    });

    const result = await response.json();

    if (result.success) {
      alert(result.message);
      location.reload();
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error toggling sticky:", error);
    alert("An error occurred. Please try again.");
  }
}

// Toggle Lock
async function toggleLock(postId) {
  try {
    const formData = new FormData();
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

    const response = await fetch(`/Forum/ToggleLock/${postId}`, {
      method: "POST",
      body: formData,
    });

    const result = await response.json();

    if (result.success) {
      alert(result.message);
      location.reload();
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error toggling lock:", error);
    alert("An error occurred. Please try again.");
  }
}

// Character counter for textareas
document.addEventListener("DOMContentLoaded", function () {
  const textareas = document.querySelectorAll("textarea[maxlength]");

  textareas.forEach((textarea) => {
    const maxLength = textarea.getAttribute("maxlength");
    if (maxLength) {
      const counter = document.createElement("small");
      counter.className = "form-text text-muted char-counter";
      counter.textContent = `0 / ${maxLength} characters`;
      textarea.parentNode.appendChild(counter);

      textarea.addEventListener("input", function () {
        const currentLength = this.value.length;
        counter.textContent = `${currentLength} / ${maxLength} characters`;

        if (currentLength >= maxLength * 0.9) {
          counter.classList.add("text-warning");
        } else {
          counter.classList.remove("text-warning");
        }
      });
    }
  });
});

// Auto-resize textareas
document.addEventListener("DOMContentLoaded", function () {
  const textareas = document.querySelectorAll("textarea");

  textareas.forEach((textarea) => {
    textarea.addEventListener("input", function () {
      this.style.height = "auto";
      this.style.height = this.scrollHeight + "px";
    });
  });
});

// Confirm before leaving page with unsaved changes
let formChanged = false;

document.addEventListener("DOMContentLoaded", function () {
  const forms = document.querySelectorAll("form");

  forms.forEach((form) => {
    const inputs = form.querySelectorAll("input, textarea, select");

    inputs.forEach((input) => {
      input.addEventListener("change", function () {
        formChanged = true;
      });
    });

    form.addEventListener("submit", function () {
      formChanged = false;
    });
  });

  window.addEventListener("beforeunload", function (e) {
    if (formChanged) {
      e.preventDefault();
      e.returnValue = "";
    }
  });
});

// Post Preview Functions
function switchToEditMode() {
  document.getElementById("edit-mode").style.display = "block";
  document.getElementById("preview-mode").style.display = "none";
  document.getElementById("edit-tab-btn").classList.add("active");
  document.getElementById("preview-tab-btn").classList.remove("active");
}

function switchToPreviewMode() {
  const title = document.getElementById("Title")?.value || "";

  // Get content from TinyMCE if available, otherwise from textarea
  let content = "";
  if (typeof tinymce !== "undefined" && tinymce.get("post-content")) {
    content = tinymce.get("post-content").getContent();
  } else {
    content = document.getElementById("post-content")?.value || "";
    // Convert line breaks to <br> for plain text
    content = content.replace(/\n/g, "<br>");
  }

  const imageInput = document.getElementById("image-upload");

  // Update preview title
  document.getElementById("preview-title").textContent =
    title || "Untitled Post";

  // Update preview content
  document.getElementById("preview-content").innerHTML = content;

  // Update preview images
  const previewImagesContainer = document.getElementById("preview-images");
  previewImagesContainer.innerHTML = "";

  if (imageInput && imageInput.files.length > 0) {
    Array.from(imageInput.files).forEach((file) => {
      if (file.type.startsWith("image/")) {
        const reader = new FileReader();
        reader.onload = function (e) {
          const img = document.createElement("img");
          img.src = e.target.result;
          previewImagesContainer.appendChild(img);
        };
        reader.readAsDataURL(file);
      }
    });
  }

  // Switch to preview mode
  document.getElementById("edit-mode").style.display = "none";
  document.getElementById("preview-mode").style.display = "block";
  document.getElementById("edit-tab-btn").classList.remove("active");
  document.getElementById("preview-tab-btn").classList.add("active");
}

// Drag and Drop File Upload
function setupDragAndDrop() {
  // Setup for all drag-drop zones
  const dropZones = document.querySelectorAll(".drag-drop-zone");

  dropZones.forEach((zone) => {
    const input = zone.querySelector(".drag-drop-input");
    if (!input) return;

    // Prevent default drag behaviors
    ["dragenter", "dragover", "dragleave", "drop"].forEach((eventName) => {
      zone.addEventListener(eventName, preventDefaults, false);
      document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop zone when item is dragged over it
    ["dragenter", "dragover"].forEach((eventName) => {
      zone.addEventListener(eventName, () => {
        zone.classList.add("drag-over");
      });
    });

    ["dragleave", "drop"].forEach((eventName) => {
      zone.addEventListener(eventName, () => {
        zone.classList.remove("drag-over");
      });
    });

    // Handle dropped files
    zone.addEventListener("drop", (e) => {
      const dt = e.dataTransfer;
      const files = dt.files;

      // Set files to input element
      input.files = files;

      // Trigger change event
      const event = new Event("change", { bubbles: true });
      input.dispatchEvent(event);
    });

    // Click on zone to open file browser
    zone.addEventListener("click", (e) => {
      if (e.target === zone || e.target.closest(".drag-drop-content")) {
        input.click();
      }
    });
  });
}

function preventDefaults(e) {
  e.preventDefault();
  e.stopPropagation();
}

// Upload Progress Indicator
function showUploadProgress() {
  const overlay = document.getElementById("upload-progress-overlay");
  const progressBar = document.getElementById("upload-progress-bar");
  const statusText = document.getElementById("upload-status");

  if (!overlay || !progressBar || !statusText) return;

  overlay.style.display = "flex";

  let progress = 0;
  const interval = setInterval(() => {
    progress += Math.random() * 15;
    if (progress > 90) progress = 90; // Stop at 90% until actual completion

    progressBar.style.width = progress + "%";
    progressBar.textContent = Math.round(progress) + "%";

    if (progress < 30) {
      statusText.textContent = "Uploading files...";
    } else if (progress < 60) {
      statusText.textContent = "Processing images...";
    } else {
      statusText.textContent = "Almost done...";
    }
  }, 500);

  // Store interval ID to clear it later
  window.uploadProgressInterval = interval;
}

function hideUploadProgress() {
  const overlay = document.getElementById("upload-progress-overlay");
  if (overlay) {
    overlay.style.display = "none";
  }
  if (window.uploadProgressInterval) {
    clearInterval(window.uploadProgressInterval);
  }
}

// File Upload Validation and Preview
document.addEventListener("DOMContentLoaded", function () {
  // Setup drag and drop
  setupDragAndDrop();

  // Setup form submission with progress indicator
  const createPostForm = document.getElementById("create-post-form");
  const editPostForm = document.getElementById("edit-post-form");

  if (createPostForm) {
    createPostForm.addEventListener("submit", function (e) {
      const imageInput = document.getElementById("image-upload");
      const fileInput = document.getElementById("file-upload");

      // Only show progress if files are being uploaded
      if (
        (imageInput && imageInput.files.length > 0) ||
        (fileInput && fileInput.files.length > 0)
      ) {
        showUploadProgress();
      }
    });
  }

  if (editPostForm) {
    editPostForm.addEventListener("submit", function (e) {
      const imageInput = document.getElementById("image-upload");
      const fileInput = document.getElementById("file-upload");

      // Only show progress if files are being uploaded
      if (
        (imageInput && imageInput.files.length > 0) ||
        (fileInput && fileInput.files.length > 0)
      ) {
        showUploadProgress();
      }
    });
  }

  // Image upload preview with remove functionality
  setupFilePreview("image-upload", "image-preview", "image", 5);

  // File upload preview with remove functionality
  setupFilePreview("file-upload", "file-preview", "file", 10);

  // Reply image upload preview
  setupFilePreview("reply-image-upload", "reply-image-preview", "image", 5);

  // Reply file upload preview
  setupFilePreview("reply-file-upload", "reply-file-preview", "file", 10);
});

// Setup file preview with remove functionality
function setupFilePreview(inputId, previewId, fileType, maxSizeMB) {
  const input = document.getElementById(inputId);
  if (!input) return;

  input.addEventListener("change", function (e) {
    const files = e.target.files;
    const previewContainer = document.getElementById(previewId);
    if (!previewContainer) return;

    previewContainer.innerHTML = "";
    const dataTransfer = new DataTransfer();

    Array.from(files).forEach((file, index) => {
      // Validate file size
      if (file.size > maxSizeMB * 1024 * 1024) {
        alert(`${file.name} is too large. Maximum size is ${maxSizeMB}MB.`);
        return;
      }

      // Validate file type
      let isValid = false;
      if (fileType === "image") {
        const validTypes = [
          "image/jpeg",
          "image/png",
          "image/gif",
          "image/webp",
        ];
        isValid = validTypes.includes(file.type);
      } else {
        const validExtensions = [".pdf", ".doc", ".docx", ".zip", ".rar"];
        const fileName = file.name.toLowerCase();
        isValid = validExtensions.some((ext) => fileName.endsWith(ext));
      }

      if (!isValid) {
        alert(`${file.name} is not a valid ${fileType} format.`);
        return;
      }

      // Add to DataTransfer
      dataTransfer.items.add(file);

      // Create preview
      const previewItem = document.createElement("div");
      previewItem.className = "preview-item";
      previewItem.dataset.index = dataTransfer.files.length - 1;

      if (fileType === "image") {
        const reader = new FileReader();
        reader.onload = function (e) {
          previewItem.innerHTML = `
                        <img src="${e.target.result}" alt="${file.name}">
                        <small>${file.name}</small>
                        <button type="button" class="btn btn-sm btn-danger remove-preview-btn" onclick="removePreviewFile('${inputId}', '${previewId}', ${
            dataTransfer.files.length - 1
          })">
                            <i class="bi bi-x"></i>
                        </button>
                    `;
        };
        reader.readAsDataURL(file);
      } else {
        previewItem.innerHTML = `
                    <i class="bi bi-file-earmark"></i>
                    <small>${file.name} (${formatFileSize(file.size)})</small>
                    <button type="button" class="btn btn-sm btn-danger remove-preview-btn" onclick="removePreviewFile('${inputId}', '${previewId}', ${
          dataTransfer.files.length - 1
        })">
                        <i class="bi bi-x"></i>
                    </button>
                `;
      }

      previewContainer.appendChild(previewItem);
    });

    // Update input files
    input.files = dataTransfer.files;
  });
}

// Remove file from preview
function removePreviewFile(inputId, previewId, fileIndex) {
  const input = document.getElementById(inputId);
  const previewContainer = document.getElementById(previewId);
  if (!input || !previewContainer) return;

  const dataTransfer = new DataTransfer();
  const files = Array.from(input.files);

  // Add all files except the one to remove
  files.forEach((file, index) => {
    if (index !== fileIndex) {
      dataTransfer.items.add(file);
    }
  });

  // Update input files
  input.files = dataTransfer.files;

  // Trigger change event to refresh preview
  const event = new Event("change", { bubbles: true });
  input.dispatchEvent(event);
}

// Format file size helper
function formatFileSize(bytes) {
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
}

// Image Gallery Navigation
function navigateGallery(galleryId, direction) {
  const gallery = document.querySelector(`[data-gallery-id="${galleryId}"]`);
  if (!gallery) return;

  const slides = gallery.querySelectorAll(".gallery-slide");
  const currentSlide = gallery.querySelector(".gallery-slide.active");
  const currentIndex = parseInt(currentSlide.getAttribute("data-slide-index"));

  let newIndex = currentIndex + direction;

  // Wrap around
  if (newIndex < 0) {
    newIndex = slides.length - 1;
  } else if (newIndex >= slides.length) {
    newIndex = 0;
  }

  // Update active slide
  currentSlide.classList.remove("active");
  slides[newIndex].classList.add("active");

  // Update counter
  const counter = gallery.querySelector(".current-slide");
  if (counter) {
    counter.textContent = newIndex + 1;
  }
}

// Delete Attachment
async function deleteAttachment(attachmentId) {
  if (!confirm("Are you sure you want to delete this attachment?")) {
    return;
  }

  try {
    const response = await fetch(`/Forum/DeleteAttachment/${attachmentId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: getAntiForgeryToken(),
      },
    });

    const result = await response.json();

    if (result.success) {
      // Remove the attachment element from the DOM
      const attachmentElement = document.getElementById(
        `attachment-${attachmentId}`
      );
      if (attachmentElement) {
        attachmentElement.remove();
      }

      // Show success message
      alert(result.message);
    } else {
      alert(result.message);
    }
  } catch (error) {
    console.error("Error deleting attachment:", error);
    alert("An error occurred while deleting the attachment.");
  }
}
