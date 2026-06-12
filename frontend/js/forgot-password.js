(function () {
  const form = document.getElementById('forgot-form');
  const result = document.getElementById('forgot-result');

  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    result.hidden = false;
    result.className = 'result';
    result.textContent = 'Sending request…';

    try {
      const email = document.getElementById('forgotEmail').value.trim();
      const response = await window.HomemadeCookieApi.forgotPassword({ email });
      result.className = 'result success';
      result.textContent = response.message || 'If that email exists, a reset hint has been sent.';
    } catch (err) {
      result.className = 'result error';
      result.textContent = err.message;
    }
  });
})();
