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
    Oatmeal: [{ value: 'Oatmeal', label: 'Oatmeal Raisin' }],
    Salty: [{ value: 'PeanutButter', label: 'Peanut Butter' }]
  };

  const COOKIE_IMAGE_URLS = {
    Chocolate: '/images/chocolate-chip.jfif',
    DarkChocolate: '/images/dark-chocolate.jfif',
    Strawberry: '/images/strawberry.jfif',
    Orange: '/images/orange-zest.jfif',
    Oatmeal: '/images/oatmeal-raisin.jpg',
    PeanutButter: '/images/peanut-butter.jfif'
  };

  const factorySelect = document.getElementById('factoryKey');
  const cookieTypeSelect = document.getElementById('cookieType');
  const imageUrlInput = document.getElementById('imageUrl');
  const form = document.getElementById('add-cookie-form');
  const resultBox = document.getElementById('result');

  function populateCookieTypes(factoryKey) {
    const types = FACTORY_COOKIE_TYPES[factoryKey] || FACTORY_COOKIE_TYPES.Chocolate;
    cookieTypeSelect.innerHTML = '';

    types.forEach((type) => {
      const option = document.createElement('option');
      option.value = type.value;
      option.textContent = type.label;
      cookieTypeSelect.appendChild(option);
    });

    cookieTypeSelect.disabled = types.length === 0;
    setImageUrlForType(cookieTypeSelect.value);
  }

  function setImageUrlForType(cookieType) {
    const defaultUrl = COOKIE_IMAGE_URLS[cookieType] ?? '/images/cookie-default.svg';
    imageUrlInput.value = defaultUrl;
    imageUrlInput.placeholder = defaultUrl;
  }

  factorySelect.addEventListener('change', () => {
    populateCookieTypes(factorySelect.value);
  });

  cookieTypeSelect.addEventListener('change', () => {
    setImageUrlForType(cookieTypeSelect.value);
  });

  async function loadCategories() {
    try {
      const categories = await window.HomemadeCookieApi.getAdminCategories();
      const categorySelect = document.getElementById('categoryId');
      categorySelect.innerHTML = categories.map((category) => `
        <option value="${category.categoryId}">${category.name}</option>
      `).join('');
    } catch (err) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = 'Unable to load categories. Please refresh.';
    }
  }

  await loadCategories();
  populateCookieTypes(factorySelect.value);

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    resultBox.hidden = false;
    resultBox.className = 'result';
    resultBox.textContent = 'Creating cookie via factory...';

    const payload = {
      factoryKey: factorySelect.value,
      cookieType: cookieTypeSelect.value,
      description: document.getElementById('description').value.trim() || null,
      imageUrl: document.getElementById('imageUrl').value.trim() || null,
      price: Number(document.getElementById('price').value),
      stock: Number(document.getElementById('stock').value),
      categoryId: Number(document.getElementById('categoryId').value)
    };

    try {
      const result = await window.HomemadeCookieApi.createCookie(payload);
      resultBox.className = 'result success';
      resultBox.innerHTML = `
        <strong>Cookie added (ID ${result.cookieId})</strong><br>
        ${result.name} — RM ${Number(result.price).toFixed(2)}, stock ${result.stock}
      `;
      form.reset();
      factorySelect.value = 'Chocolate';
      populateCookieTypes('Chocolate');
    } catch (error) {
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    }
  });
})();
