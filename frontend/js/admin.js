(async function () {
  window.HomemadeCookieAuth.requireAdmin();

  const FACTORY_COOKIE_TYPES = {
    Chocolate: [
      { value: 'Chocolate', label: 'Chocolate Chip' },
      { value: 'DarkChocolate', label: 'Dark Chocolate' }
    ],
    Fruit: [
      { value: 'Strawberry', label: 'Strawberry' },
      { value: 'Orange', label: 'Orange' }
    ],
    Oatmeal: [
      { value: 'Oatmeal', label: 'Oatmeal Raisin' }
    ],
    Salty: [
      { value: 'PeanutButter', label: 'Peanut Butter' }
    ]
  };

  const factorySelect = document.getElementById('factoryKey');
  const cookieTypeSelect = document.getElementById('cookieType');
  const imageFileInput = document.getElementById('imageFile');
  const imagePreview = document.getElementById('imagePreview');

  const form = document.getElementById('add-cookie-form');
  const resultBox = document.getElementById('result');
  const categorySelect = document.getElementById('categoryId');

  // -----------------------------
  // Populate cookie types
  // -----------------------------
  function populateCookieTypes(factoryKey) {
    const types = FACTORY_COOKIE_TYPES[factoryKey] || FACTORY_COOKIE_TYPES.Chocolate;

    cookieTypeSelect.innerHTML = '';

    types.forEach(type => {
      const option = document.createElement('option');
      option.value = type.value;
      option.textContent = type.label;
      cookieTypeSelect.appendChild(option);
    });
  }

  factorySelect.addEventListener('change', () => {
    populateCookieTypes(factorySelect.value);
  });

  // -----------------------------
  // Image preview (file only)
  // -----------------------------
  imageFileInput.addEventListener('change', () => {
    const file = imageFileInput.files[0];

    if (!file) {
      imagePreview.style.display = 'none';
      imagePreview.src = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = e => {
      imagePreview.src = e.target.result;
      imagePreview.style.display = 'block';
    };

    reader.readAsDataURL(file);
  });

  // -----------------------------
  // Load categories
  // -----------------------------
  async function loadCategories() {
    try {
      const categories = await window.HomemadeCookieApi.getAdminCategories();

      categorySelect.innerHTML = categories
        .map(c => `<option value="${c.categoryId}">${c.name}</option>`)
        .join('');
    } catch (err) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = 'Failed to load categories';
    }
  }

  await loadCategories();
  populateCookieTypes(factorySelect.value);

  // -----------------------------
  // Submit form (UPLOAD FILE)
  // -----------------------------
  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const file = imageFileInput.files[0];

    if (!file) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = 'Please select an image file';
      return;
    }

    resultBox.hidden = false;
    resultBox.className = 'result';
    resultBox.textContent = 'Uploading image and creating cookie...';

    const formData = new FormData();

    formData.append('name', document.getElementById('editCookieName').value.trim());
    formData.append('description', document.getElementById('editCookieDescription').value.trim());
    formData.append('price', document.getElementById('editCookiePrice').value);
    formData.append('stock', document.getElementById('editCookieStock').value);
    formData.append('categoryId', document.getElementById('editCookieCategoryId').value);

    const imageFile = document.getElementById('editCookieImageFile').files[0];

    if (imageFile) {
      formData.append('image', imageFile);
    }

    await updateCookie(cookieId, formData);

    try {
      const response = await fetch('/api/products', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        throw new Error('Upload failed');
      }

      const data = await response.json();

      resultBox.className = 'result success';
      resultBox.innerHTML = `
        <strong>Cookie created successfully!</strong><br>
        ID: ${data.cookieId}
      `;

      form.reset();
      imagePreview.style.display = 'none';
      imagePreview.src = '';
      factorySelect.value = 'Chocolate';
      populateCookieTypes('Chocolate');

    } catch (err) {
      resultBox.className = 'result error';
      resultBox.textContent = err.message;
    }
  });

})();