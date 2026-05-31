(async function () {
  const user = await window.HomemadeCookieAuth.requireAdmin();
  if (!user) return;

  const form = document.getElementById('category-form');
  const result = document.getElementById('category-result');
  const list = document.getElementById('category-list');

  function renderCategories(categories) {
    list.innerHTML = categories.map((category) => `
      <tr>
        <td>${category.categoryId}</td>
        <td>${category.name}</td>
        <td>
          <button class="btn-secondary" data-action="edit" data-id="${category.categoryId}" data-name="${category.name}">Edit</button>
          <button class="btn-secondary" data-action="delete" data-id="${category.categoryId}">Delete</button>
        </td>
      </tr>
    `).join('');
  }

  async function loadCategories() {
    try {
      const categories = await window.HomemadeCookieApi.getAdminCategories();
      renderCategories(categories);
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

    const action = button.dataset.action;
    const categoryId = Number(button.dataset.id);

    if (action === 'edit') {
      const name = button.dataset.name;
      const newName = window.prompt('Update category name', name);
      if (!newName || newName.trim() === '') return;
      try {
        await window.HomemadeCookieApi.updateCategory(categoryId, { name: newName.trim() });
        await loadCategories();
        await showMessage('Category updated successfully.');
      } catch (err) {
        await showMessage(err.message, false);
      }
    }

    if (action === 'delete') {
      const confirmed = window.confirm('Delete this category? This cannot be undone.');
      if (!confirmed) return;
      try {
        await window.HomemadeCookieApi.deleteCategory(categoryId);
        await loadCategories();
        await showMessage('Category deleted.');
      } catch (err) {
        await showMessage(err.message, false);
      }
    }
  });

  form?.addEventListener('submit', async (event) => {
    event.preventDefault();
    const name = document.getElementById('categoryName').value.trim();
    if (!name) return;

    try {
      await window.HomemadeCookieApi.createCategory({ name });
      document.getElementById('categoryName').value = '';
      await loadCategories();
      await showMessage('Category created successfully.');
    } catch (err) {
      await showMessage(err.message, false);
    }
  });

  loadCategories();
})();
