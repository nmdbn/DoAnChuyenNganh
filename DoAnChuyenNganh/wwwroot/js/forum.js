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
  }
}

// Cancel Reply Edit
function cancelReplyEdit(replyId) {
  const replyBody = document.getElementById(`reply-body-${replyId}`);
  const editForm = document.getElementById(`reply-edit-form-${replyId}`);

  if (replyBody && editForm) {
    replyBody.style.display = "block";
    editForm.style.display = "none";
  }
}

// Save Reply Edit
async function saveReplyEdit(replyId) {
  const contentTextarea = document.getElementById(
    `reply-edit-content-${replyId}`
  );
  if (!contentTextarea) return;

  const content = contentTextarea.value;

  try {
    const formData = new FormData();
    formData.append("ReplyId", replyId);
    formData.append("Content", content);
    formData.append("__RequestVerificationToken", getAntiForgeryToken());

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
  const content = document.getElementById("post-content")?.value || "";
  const imageInput = document.getElementById("image-upload");

  // Update preview title
  document.getElementById("preview-title").textContent =
    title || "Untitled Post";

  // Update preview content (convert line breaks to <br>)
  document.getElementById("preview-content").innerHTML = content.replace(
    /\n/g,
    "<br>"
  );

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

// File Upload Validation and Preview
document.addEventListener("DOMContentLoaded", function () {
  // Image upload preview
  const imageUpload = document.getElementById("image-upload");
  if (imageUpload) {
    imageUpload.addEventListener("change", function (e) {
      const files = e.target.files;
      const previewContainer = document.getElementById("image-preview");
      if (previewContainer) {
        previewContainer.innerHTML = "";

        Array.from(files).forEach((file) => {
          // Validate file size (5MB)
          if (file.size > 5 * 1024 * 1024) {
            alert(`${file.name} is too large. Maximum size is 5MB.`);
            return;
          }

          // Validate file type
          const validTypes = [
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
          ];
          if (!validTypes.includes(file.type)) {
            alert(`${file.name} is not a valid image format.`);
            return;
          }

          // Create preview
          const reader = new FileReader();
          reader.onload = function (e) {
            const previewItem = document.createElement("div");
            previewItem.className = "preview-item";
            previewItem.innerHTML = `
                            <img src="${e.target.result}" alt="${file.name}">
                            <small>${file.name}</small>
                        `;
            previewContainer.appendChild(previewItem);
          };
          reader.readAsDataURL(file);
        });
      }
    });
  }

  // File upload preview
  const fileUpload = document.getElementById("file-upload");
  if (fileUpload) {
    fileUpload.addEventListener("change", function (e) {
      const files = e.target.files;
      const previewContainer = document.getElementById("file-preview");
      if (previewContainer) {
        previewContainer.innerHTML = "";

        Array.from(files).forEach((file) => {
          // Validate file size (10MB)
          if (file.size > 10 * 1024 * 1024) {
            alert(`${file.name} is too large. Maximum size is 10MB.`);
            return;
          }

          // Validate file type
          const validExtensions = [".pdf", ".doc", ".docx", ".zip", ".rar"];
          const fileName = file.name.toLowerCase();
          const isValid = validExtensions.some((ext) => fileName.endsWith(ext));

          if (!isValid) {
            alert(`${file.name} is not a valid file format.`);
            return;
          }

          // Create preview
          const previewItem = document.createElement("div");
          previewItem.className = "preview-item";
          previewItem.innerHTML = `
                        <i class="bi bi-file-earmark"></i>
                        <small>${file.name} (${formatFileSize(
            file.size
          )})</small>
                    `;
          previewContainer.appendChild(previewItem);
        });
      }
    });
  }
});

// Format file size helper
function formatFileSize(bytes) {
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
}
