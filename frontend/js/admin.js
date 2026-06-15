// admin.js - Complete working version
(async function () {
  // Check if user is admin
  const user = await window.HomemadeCookieAuth.requireAdmin();
  if (!user) return;

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

    if (!cookieTypeSelect) return;
    
    cookieTypeSelect.innerHTML = '';

    types.forEach(type => {
      const option = document.createElement('option');
      option.value = type.value;
      option.textContent = type.label;
      cookieTypeSelect.appendChild(option);
    });
  }

  if (factorySelect) {
    factorySelect.addEventListener('change', () => {
      populateCookieTypes(factorySelect.value);
    });
  }

  // -----------------------------
  // Image preview (file only)
  // -----------------------------
  if (imageFileInput) {
    imageFileInput.addEventListener('change', () => {
      const file = imageFileInput.files[0];

      if (!file) {
        if (imagePreview) {
          imagePreview.style.display = 'none';
          imagePreview.src = '';
        }
        return;
      }

      const reader = new FileReader();
      reader.onload = e => {
        if (imagePreview) {
          imagePreview.src = e.target.result;
          imagePreview.style.display = 'block';
        }
      };

      reader.readAsDataURL(file);
    });
  }

  // -----------------------------
  // Load categories
  // -----------------------------
  async function loadCategories() {
    try {
      const categories = await window.HomemadeCookieApi.getAdminCategories();
      
      if (categorySelect) {
        categorySelect.innerHTML = '<option value="">Select Category</option>' +
          categories
            .map(c => `<option value="${c.categoryId || c.id}">${c.name}</option>`)
            .join('');
      }
    } catch (err) {
      console.error('Failed to load categories:', err);
      if (resultBox) {
        resultBox.hidden = false;
        resultBox.className = 'result error';
        resultBox.textContent = 'Failed to load categories';
      }
    }
  }

  await loadCategories();
  
  if (factorySelect) {
    populateCookieTypes(factorySelect.value);
  }

  // -----------------------------
  // Submit form (UPLOAD FILE)
  // -----------------------------
  if (form) {
    form.addEventListener('submit', async (event) => {
      event.preventDefault();

      // Get form elements
      const nameInput = document.getElementById('name');
      const descriptionInput = document.getElementById('description');
      const priceInput = document.getElementById('price');
      const stockInput = document.getElementById('stock');
      const categoryIdInput = document.getElementById('categoryId');
      const imageFileInputElem = document.getElementById('imageFile');
      const factoryKeyInput = document.getElementById('factoryKey');
      const cookieTypeInput = document.getElementById('cookieType');

      // Get values with fallbacks for missing fields
      let name = '';
      if (nameInput) {
        name = nameInput.value.trim();
      } else {
        // Generate a name from cookie type if name field doesn't exist
        const cookieType = cookieTypeInput?.value || 'Cookie';
        name = `${cookieType} Cookie ${new Date().toLocaleTimeString()}`;
        console.warn('Name field not found, using generated name:', name);
      }
      
      const description = descriptionInput ? descriptionInput.value.trim() : '';
      const price = priceInput ? parseFloat(priceInput.value) : 0;
      const stock = stockInput ? parseInt(stockInput.value) : 0;
      const categoryId = categoryIdInput ? categoryIdInput.value : '';
      const factoryKey = factoryKeyInput?.value || 'Chocolate';
      const cookieType = cookieTypeInput?.value || 'Chocolate';

      // Validate required fields
      if (!name) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Cookie name is required';
        }
        return;
      }

      if (!description) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Description is required';
        }
        return;
      }

      if (isNaN(price) || price <= 0) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Valid price is required (greater than 0)';
        }
        return;
      }

      if (isNaN(stock) || stock < 0) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Valid stock quantity is required';
        }
        return;
      }

      if (!categoryId) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Please select a category';
        }
        return;
      }

      const file = imageFileInputElem?.files[0];
      if (!file) {
        if (resultBox) {
          resultBox.hidden = false;
          resultBox.className = 'result error';
          resultBox.textContent = 'Please select an image file';
        }
        return;
      }

      if (resultBox) {
        resultBox.hidden = false;
        resultBox.className = 'result';
        resultBox.textContent = '📤 Uploading image and creating cookie...';
      }

      const formData = new FormData();
      formData.append('name', name);
      formData.append('description', description);
      formData.append('price', price);
      formData.append('stock', stock);
      formData.append('categoryId', categoryId);
      formData.append('factoryKey', factoryKey);
      formData.append('cookieType', cookieType);
      formData.append('image', file);

      try {
        const response = await fetch('/api/admin/cookies', {
          method: 'POST',
          credentials: 'include',
          body: formData
        });

        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(errorText || 'Upload failed');
        }

        const data = await response.json();

        if (resultBox) {
          resultBox.className = 'result success';
          resultBox.innerHTML = `
            <strong>✅ Cookie created successfully!</strong><br>
            🍪 Name: ${name}<br>
            📦 ID: ${data.cookieId || data.id}<br>
            💰 Price: RM ${price.toFixed(2)}<br>
            📊 Stock: ${stock}
          `;
        }

        // Reset form
        form.reset();
        
        if (imagePreview) {
          imagePreview.style.display = 'none';
          imagePreview.src = '';
        }
        
        if (factorySelect) {
          factorySelect.value = 'Chocolate';
          populateCookieTypes('Chocolate');
        }
        
        // Clear name field if it exists
        if (nameInput) nameInput.value = '';
        
        // Reload categories to refresh if needed
        await loadCategories();

        // Scroll to top to show success message
        window.scrollTo({ top: 0, behavior: 'smooth' });

        // Auto hide success message after 5 seconds
        setTimeout(() => {
          if (resultBox) {
            resultBox.style.display = 'none';
          }
        }, 5000);

      } catch (err) {
        console.error('Error creating cookie:', err);
        if (resultBox) {
          resultBox.className = 'result error';
          resultBox.innerHTML = `❌ Failed to create cookie: ${err.message}`;
        }
      }
    });
  }
})();