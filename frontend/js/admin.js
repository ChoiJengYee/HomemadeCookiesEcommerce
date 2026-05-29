(function () {
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

  const factorySelect = document.getElementById('factoryKey');
  const cookieTypeSelect = document.getElementById('cookieType');
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
  }

  factorySelect.addEventListener('change', () => {
    populateCookieTypes(factorySelect.value);
  });

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
