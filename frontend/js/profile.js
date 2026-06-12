(async function () {
  const user = await window.HomemadeCookieAuth.refreshFromServer();
  if (!user) {
    window.location.href = '/login.html';
    return;
  }

  if (user.role !== 'Customer') {
    if (user.role === 'Admin') {
      window.location.href = '/admin/orders.html';
      return;
    }
    window.location.href = '/login.html';
    return;
  }

  const profileForm = document.getElementById('profile-form');
  const nameInput = document.getElementById('profileName');
  const emailInput = document.getElementById('profileEmail');
  const addressInput = document.getElementById('profileAddress');
  const phoneInput = document.getElementById('profilePhone');
  const profileResult = document.getElementById('profile-result');

  if (user) {
    nameInput.value = user.name || '';
    emailInput.value = user.email || '';
    addressInput.value = user.address || '';
    phoneInput.value = user.phoneNumber || '';
  }

  profileForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    profileResult.hidden = false;
    profileResult.className = 'result';
    profileResult.textContent = 'Saving profile…';

    try {
      const updatedUser = await window.HomemadeCookieApi.updateProfile({
        name: nameInput.value.trim(),
        address: addressInput.value.trim(),
        phoneNumber: phoneInput.value.trim()
      });

      window.HomemadeCookieAuth.refreshFromServer();
      profileResult.className = 'result success';
      profileResult.textContent = 'Profile updated successfully.';
      nameInput.value = updatedUser.name;
      addressInput.value = updatedUser.address;
      phoneInput.value = updatedUser.phoneNumber;
    } catch (err) {
      profileResult.className = 'result error';
      profileResult.textContent = err.message;
    }
  });
})();
