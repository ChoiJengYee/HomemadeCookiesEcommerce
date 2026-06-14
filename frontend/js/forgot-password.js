/**
 * Direct Password Reset Page Handler
 * Integrates with existing auth.js and api.js
 */

(function() {
  const form = document.getElementById('forgot-form');
  const resultDiv = document.getElementById('forgot-result');
  const emailInput = document.getElementById('forgotEmail');
  const newPasswordInput = document.getElementById('newPassword');
  const confirmPasswordInput = document.getElementById('confirmPassword');
  const strengthDiv = document.getElementById('password-strength');
  const submitButton = form?.querySelector('button[type="submit"]');
  
  // Check if user is already logged in, redirect if so
  if (window.HomemadeCookieAuth?.getUser()) {
    const user = window.HomemadeCookieAuth.getUser();
    const dashboard = user.role === 'admin' ? '/admin/dashboard.html' : '/dashboard.html';
    window.location.href = dashboard;
    return;
  }
  
  // Function to show UI messages
  function showMessage(message, type = 'info') {
    if (!resultDiv) return;
    
    resultDiv.hidden = false;
    resultDiv.className = `result ${type}`;
    resultDiv.textContent = message;
    
    // Auto-hide messages after 5 seconds
    if (type === 'success' || type === 'error') {
      setTimeout(() => {
        if (resultDiv && !resultDiv.hidden) {
          resultDiv.style.opacity = '0';
          setTimeout(() => {
            if (resultDiv) {
              resultDiv.hidden = true;
              resultDiv.style.opacity = '';
            }
          }, 300);
        }
      }, 5000);
    }
  }
  
  // Function to set loading states on button
  function setLoading(isLoading) {
    if (!submitButton) return;
    
    if (isLoading) {
      const originalText = submitButton.textContent;
      submitButton.disabled = true;
      submitButton.innerHTML = '<span class="loading-spinner"></span> Resetting...';
      submitButton.setAttribute('data-original-text', originalText);
    } else {
      submitButton.disabled = false;
      const originalText = submitButton.getAttribute('data-original-text');
      submitButton.textContent = originalText || 'Reset Password';
    }
  }
  
  // Password strength logic
  function checkPasswordStrength(password) {
    if (!password) return { text: '', class: '' };
    
    let strength = 0;
    if (password.length >= 8) strength++;
    if (password.match(/[a-z]+/)) strength++;
    if (password.match(/[A-Z]+/)) strength++;
    if (password.match(/[0-9]+/)) strength++;
    if (password.match(/[$@#&!]+/)) strength++;
    
    switch(strength) {
      case 0:
      case 1:
        return { text: 'Weak password', class: 'weak' };
      case 2:
      case 3:
        return { text: 'Medium password', class: 'medium' };
      default:
        return { text: 'Strong password', class: 'strong' };
    }
  }
  
  // Listener for Password Strength Display
  if (newPasswordInput && strengthDiv) {
    newPasswordInput.addEventListener('input', function() {
      const password = this.value;
      const strength = checkPasswordStrength(password);
      
      strengthDiv.textContent = strength.text;
      strengthDiv.className = `password-strength ${strength.class}`;
    });
  }
  
  // Handle single-step form submission
  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      
      const email = emailInput?.value.trim();
      const newPassword = newPasswordInput?.value;
      const confirmPassword = confirmPasswordInput?.value;
      
      // 1. Email Validations
      if (!email) {
        showMessage('Please enter your email address.', 'error');
        return;
      }
      if (!email.includes('@') || !email.includes('.')) {
        showMessage('Please enter a valid email address.', 'error');
        return;
      }
      
      // 2. Password Validations
      if (!newPassword || newPassword.length < 6) {
        showMessage('Password must be at least 6 characters long.', 'error');
        return;
      }
      if (newPassword !== confirmPassword) {
        showMessage('Passwords do not match.', 'error');
        return;
      }
      
      setLoading(true);
      
      try {
        // Direct integration fetch request mapping to C# ForgotPasswordRequest / API routes
        const response = await fetch('/api/auth/reset-password', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ 
            email: email, 
            password: newPassword,
            newPassword: newPassword
          })
        });
        
        if (!response.ok) {
          const data = await response.json().catch(() => ({}));
          throw new Error(data.message || data.error || 'Failed to update password.');
        }
        
        showMessage('✅ Password reset successful! Redirecting to login...', 'success');
        
        // Clear forms
        if (emailInput) emailInput.value = '';
        if (newPasswordInput) newPasswordInput.value = '';
        if (confirmPasswordInput) confirmPasswordInput.value = '';
        if (strengthDiv) strengthDiv.textContent = '';
        
        // Redirect to Login Page
        setTimeout(() => {
          window.location.href = '/login.html';
        }, 2000);
        
      } catch (error) {
        console.error('Password reset error:', error);
        if (error.status === 429) {
          showMessage('Too many attempts. Please wait a few minutes before trying again.', 'error');
        } else {
          showMessage(error.message || 'An error occurred. Please verify your connection or system setup.', 'error');
        }
      } finally {
        setLoading(false);
      }
    });
  }
})();