// Authentication JavaScript

document.addEventListener("DOMContentLoaded", function () {
  // Password strength checker
  const passwordInputs = document.querySelectorAll(
    'input[type="password"][data-check-strength]'
  );
  passwordInputs.forEach((input) => {
    input.addEventListener("input", checkPasswordStrength);
  });

  // Confirm password validation
  const confirmPasswordInputs = document.querySelectorAll(
    "input[data-confirm-password]"
  );
  confirmPasswordInputs.forEach((input) => {
    const passwordInput = document.querySelector(input.dataset.confirmPassword);
    if (passwordInput) {
      input.addEventListener("input", function () {
        validatePasswordMatch(passwordInput, input);
      });
      passwordInput.addEventListener("input", function () {
        validatePasswordMatch(passwordInput, input);
      });
    }
  });

  // Form validation
  const authForms = document.querySelectorAll(".auth-form");
  authForms.forEach((form) => {
    form.addEventListener("submit", function (e) {
      if (!validateForm(form)) {
        e.preventDefault();
      }
    });
  });

  // Social login buttons (UI only - no backend implementation)
  const socialButtons = document.querySelectorAll(".btn-social");
  socialButtons.forEach((button) => {
    button.addEventListener("click", function (e) {
      e.preventDefault();
      const provider = this.dataset.provider;
      showSocialLoginMessage(provider);
    });
  });

  // Auto-hide alerts
  const alerts = document.querySelectorAll(".alert-auth");
  alerts.forEach((alert) => {
    setTimeout(() => {
      fadeOut(alert);
    }, 5000);
  });
});

// Password strength checker
function checkPasswordStrength(e) {
  const password = e.target.value;
  const strengthContainer =
    e.target.parentElement.querySelector(".password-strength");

  if (!strengthContainer) return;

  const strengthBar = strengthContainer.querySelector(".password-strength-bar");
  const strengthText = e.target.parentElement.querySelector(
    ".password-strength-text"
  );

  if (password.length === 0) {
    strengthBar.className = "password-strength-bar";
    strengthBar.style.width = "0%";
    if (strengthText) strengthText.textContent = "";
    return;
  }

  let strength = 0;
  let strengthLabel = "";

  // Length check
  if (password.length >= 8) strength++;
  if (password.length >= 12) strength++;

  // Character variety checks
  if (/[a-z]/.test(password)) strength++;
  if (/[A-Z]/.test(password)) strength++;
  if (/[0-9]/.test(password)) strength++;
  if (/[^a-zA-Z0-9]/.test(password)) strength++;

  // Determine strength level
  if (strength <= 2) {
    strengthBar.className = "password-strength-bar password-strength-weak";
    strengthLabel = "Weak";
  } else if (strength <= 4) {
    strengthBar.className = "password-strength-bar password-strength-medium";
    strengthLabel = "Medium";
  } else {
    strengthBar.className = "password-strength-bar password-strength-strong";
    strengthLabel = "Strong";
  }

  if (strengthText) {
    strengthText.textContent = `Password strength: ${strengthLabel}`;
  }
}

// Validate password match
function validatePasswordMatch(passwordInput, confirmInput) {
  const password = passwordInput.value;
  const confirmPassword = confirmInput.value;

  if (confirmPassword.length === 0) {
    confirmInput.classList.remove("is-invalid");
    return;
  }

  if (password !== confirmPassword) {
    confirmInput.classList.add("is-invalid");
    showFieldError(confirmInput, "Passwords do not match");
  } else {
    confirmInput.classList.remove("is-invalid");
    hideFieldError(confirmInput);
  }
}

// Form validation
function validateForm(form) {
  let isValid = true;
  const requiredFields = form.querySelectorAll("[required]");

  requiredFields.forEach((field) => {
    if (!field.value.trim()) {
      field.classList.add("is-invalid");
      showFieldError(field, "This field is required");
      isValid = false;
    } else {
      field.classList.remove("is-invalid");
      hideFieldError(field);
    }
  });

  // Email validation
  const emailFields = form.querySelectorAll('input[type="email"]');
  emailFields.forEach((field) => {
    if (field.value && !isValidEmail(field.value)) {
      field.classList.add("is-invalid");
      showFieldError(field, "Please enter a valid email address");
      isValid = false;
    }
  });

  // Password confirmation validation
  const confirmPasswordFields = form.querySelectorAll(
    "input[data-confirm-password]"
  );
  confirmPasswordFields.forEach((confirmField) => {
    const passwordField = form.querySelector(
      confirmField.dataset.confirmPassword
    );
    if (passwordField && confirmField.value !== passwordField.value) {
      confirmField.classList.add("is-invalid");
      showFieldError(confirmField, "Passwords do not match");
      isValid = false;
    }
  });

  return isValid;
}

// Email validation
function isValidEmail(email) {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

// Show field error
function showFieldError(field, message) {
  let errorElement = field.parentElement.querySelector(
    ".field-validation-error"
  );
  if (!errorElement) {
    errorElement = document.createElement("span");
    errorElement.className = "text-danger field-validation-error";
    field.parentElement.appendChild(errorElement);
  }
  errorElement.textContent = message;
}

// Hide field error
function hideFieldError(field) {
  const errorElement = field.parentElement.querySelector(
    ".field-validation-error"
  );
  if (errorElement) {
    errorElement.remove();
  }
}

// Social login message (UI only)
function showSocialLoginMessage(provider) {
  const message = `Social login with ${provider} is not yet implemented. This is a UI placeholder only.`;
  alert(message);
}

// Fade out animation
function fadeOut(element) {
  let opacity = 1;
  const timer = setInterval(function () {
    if (opacity <= 0.1) {
      clearInterval(timer);
      element.style.display = "none";
    }
    element.style.opacity = opacity;
    opacity -= opacity * 0.1;
  }, 50);
}

// Real-time username validation
function checkUsernameAvailability(username) {
  if (username.length < 3) return;

  // This would normally make an AJAX call to check username availability
  // For now, it's just a placeholder
  console.log("Checking username availability:", username);
}

// Real-time email validation
function checkEmailAvailability(email) {
  if (!isValidEmail(email)) return;

  // This would normally make an AJAX call to check email availability
  // For now, it's just a placeholder
  console.log("Checking email availability:", email);
}

// Toggle password visibility
function togglePasswordVisibility(buttonElement) {
  const input = buttonElement.previousElementSibling;
  if (input && input.type === "password") {
    input.type = "text";
    buttonElement.innerHTML = '<i class="bi bi-eye-slash"></i>';
  } else if (input) {
    input.type = "password";
    buttonElement.innerHTML = '<i class="bi bi-eye"></i>';
  }
}

// Profile image preview
function previewProfileImage(input) {
  if (input.files && input.files[0]) {
    const reader = new FileReader();
    reader.onload = function (e) {
      const preview = document.querySelector(".profile-avatar");
      if (preview) {
        preview.src = e.target.result;
      }
    };
    reader.readAsDataURL(input.files[0]);
  }
}

// Avatar upload preview
function previewAvatar(input) {
  if (input.files && input.files[0]) {
    const file = input.files[0];

    // Validate file size (2MB)
    if (file.size > 2 * 1024 * 1024) {
      alert("Avatar file is too large. Maximum size is 2MB.");
      input.value = "";
      return;
    }

    // Validate file type
    const validTypes = ["image/jpeg", "image/png", "image/gif"];
    if (!validTypes.includes(file.type)) {
      alert("Invalid file format. Please use JPG, PNG, or GIF.");
      input.value = "";
      return;
    }

    // Show preview
    const reader = new FileReader();
    reader.onload = function (e) {
      const previewContainer = document.getElementById("avatar-preview");
      const existingImg = document.getElementById("avatar-preview-img");
      const placeholder = document.getElementById("avatar-placeholder");

      if (existingImg) {
        existingImg.src = e.target.result;
      } else if (placeholder) {
        placeholder.style.display = "none";
        const newImg = document.createElement("img");
        newImg.id = "avatar-preview-img";
        newImg.src = e.target.result;
        newImg.alt = "Avatar Preview";
        previewContainer.appendChild(newImg);
      } else {
        previewContainer.innerHTML = `<img id="avatar-preview-img" src="${e.target.result}" alt="Avatar Preview" />`;
      }
    };
    reader.readAsDataURL(file);
  }
}
