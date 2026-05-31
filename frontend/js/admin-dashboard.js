(async function () {
  const user = await window.HomemadeCookieAuth.requireAdmin();
  if (!user) return;
})();
