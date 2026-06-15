(async function () {
  const user = await window.HomemadeCookieAuth.requireAdmin();
  if (!user) return;

  const startDateInput = document.getElementById("sales-start-date");
  const endDateInput = document.getElementById("sales-end-date");
  const loadBtn = document.getElementById("load-sales-chart");
  const messageEl = document.getElementById("sales-chart-message");

  const revenueEl = document.getElementById("dashboard-total-revenue");
  const quantityEl = document.getElementById("dashboard-total-quantity");
  const bestSellerEl = document.getElementById("dashboard-best-seller");
  const cookieCountEl = document.getElementById("dashboard-cookie-count");

  let salesChart = null;

  function formatPrice(amount) {
    return `RM ${Number(amount || 0).toFixed(2)}`;
  }

  const today = new Date();
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);

  startDateInput.value = firstDay.toISOString().split("T")[0];
  endDateInput.value = today.toISOString().split("T")[0];

  async function loadSalesChart() {
    try {
      messageEl.textContent = "Loading sales chart...";

      const data = await window.HomemadeCookieApi.getAdminSalesData(
        startDateInput.value,
        endDateInput.value
      );

      const salesData = Array.isArray(data) ? data : [];

      const labels = salesData.map(item => item.cookieName);
      const sales = salesData.map(item => Number(item.totalSales || 0));

      const totalRevenue = salesData.reduce((sum, item) => sum + Number(item.totalSales || 0), 0);
      const totalQuantity = salesData.reduce((sum, item) => sum + Number(item.totalQuantity || 0), 0);
      const bestSeller = salesData.length > 0 ? salesData[0].cookieName : "-";

      revenueEl.textContent = formatPrice(totalRevenue);
      quantityEl.textContent = totalQuantity;
      bestSellerEl.textContent = bestSeller;
      cookieCountEl.textContent = salesData.length;

      const ctx = document.getElementById("cookie-sales-chart");

      if (salesChart) {
        salesChart.destroy();
      }

      salesChart = new Chart(ctx, {
        type: "bar",
        data: {
          labels,
          datasets: [
            {
              label: "Total Sales (RM)",
              data: sales
            }
          ]
        },
        options: {
          responsive: true,
          plugins: {
            legend: {
              display: true
            }
          },
          scales: {
            y: {
              beginAtZero: true
            }
          }
        }
      });

      messageEl.textContent = salesData.length
        ? "Sales chart loaded successfully."
        : "No sales data found for this timeline.";

    } catch (err) {
      console.error(err);
      messageEl.textContent = "Failed to load sales chart: " + err.message;
    }
  }

  loadBtn.addEventListener("click", loadSalesChart);

  loadSalesChart();
})();