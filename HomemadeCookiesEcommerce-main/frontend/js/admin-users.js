(async function () {
  const user = await window.HomemadeCookieAuth.requireAdmin();
  if (!user) return;

  const result = document.getElementById('user-result');
  const list = document.getElementById('user-list');

  function renderUsers(users) {
    list.innerHTML = users.map((user) => `
      <tr>
        <td>${user.userId}</td>
        <td>${user.name}</td>
        <td>${user.email}</td>
        <td>${user.role}</td>
        <td>${user.address || '—'}</td>
        <td>${user.phoneNumber || '—'}</td>
        <td>
          <button class="btn-secondary" data-action="promote" data-id="${user.userId}" data-role="${user.role}">
            ${user.role === 'Admin' ? 'Make customer' : 'Make admin'}
          </button>
        </td>
      </tr>
    `).join('');
  }

  async function loadUsers() {
    try {
      const users = await window.HomemadeCookieApi.getAdminUsers();
      renderUsers(users);
    } catch (err) {
      result.hidden = false;
      result.className = 'result error';
      result.textContent = err.message;
    }
  }

  async function showMessage(message, success = true) {
    result.hidden = false;
    result.className = success ? 'result success' : 'result error';
    result.textContent = message;
    setTimeout(() => {
      result.hidden = true;
    }, 3500);
  }

  list.addEventListener('click', async (event) => {
    const button = event.target.closest('button[data-action]');
    if (!button) return;

    const userId = Number(button.dataset.id);
    const currentRole = button.dataset.role;
    const nextRole = currentRole === 'Admin' ? 'Customer' : 'Admin';

    try {
      await window.HomemadeCookieApi.updateUserRole(userId, { role: nextRole });
      await loadUsers();
      await showMessage('User role updated.');
    } catch (err) {
      await showMessage(err.message, false);
    }
  });

  loadUsers();
})();
