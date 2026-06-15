// admin-reports.js - Fully corrected version with proper item names
document.addEventListener("DOMContentLoaded", async function () {
  console.log("=== admin-reports.js LOADED ===");

  const filterForm = document.getElementById("report-filter-form");
  const btnFetch = document.getElementById("btn-fetch-report");

  const startDateInput = document.getElementById("startDate");
  const endDateInput = document.getElementById("endDate");

  const resultBox = document.getElementById("result");
  const exportControls = document.getElementById("export-controls");
  const previewContainer = document.getElementById("report-preview-container");
  const tableBody = document.getElementById("report-table-body");
  const grossSumCell = document.getElementById("report-gross-sum");

  const btnExportPdf = document.getElementById("btn-export-pdf");
  const btnExportCsv = document.getElementById("btn-export-csv");

  let localReportCache = [];

  function updateStatus(text, isError = false) {
    if (!resultBox) return;
    resultBox.hidden = false;
    resultBox.className = isError ? "result error" : "result success";
    resultBox.textContent = text;
  }

  const today = new Date();
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);

  if (startDateInput && endDateInput) {
    startDateInput.value = firstDay.toISOString().split("T")[0];
    endDateInput.value = today.toISOString().split("T")[0];
  }

  // Helper function to extract item name from various possible structures
  function getItemName(item) {
    if (!item) return 'Unknown Item';
    
    // Try all possible property names that might contain the product name
    const possibleNames = [
      item.name,
      item.productName,
      item.cookieName,
      item.itemName,
      item.title,
      item.ProductName,
      item.CookieName,
      item.product?.name,
      item.cookie?.name,
      item.product?.productName,
      item.product?.ProductName
    ];
    
    // Find the first valid name
    for (const name of possibleNames) {
      if (name && typeof name === 'string' && name.trim()) {
        return name.trim();
      }
    }
    
    // If we have an ID but no name, show that
    if (item.productId) return `Product #${item.productId}`;
    if (item.cookieId) return `Cookie #${item.cookieId}`;
    if (item.id) return `Item #${item.id}`;
    
    return 'Unknown Item';
  }

  // Helper function to extract quantity
  function getItemQuantity(item) {
    if (!item) return 1;
    return item.quantity || item.qty || item.Quantity || 1;
  }

  async function fetchReport() {
    updateStatus("Loading report...");

    if (exportControls) exportControls.style.display = "none";
    if (previewContainer) previewContainer.style.display = "none";
    if (tableBody) tableBody.innerHTML = "";
    localReportCache = [];

    try {
      const allOrders = await window.HomemadeCookieApi.getAdminOrders(
        startDateInput.value,
        endDateInput.value
      );

      const orders = Array.isArray(allOrders) ? allOrders : [];
      
      if (orders.length === 0) {
        updateStatus("No orders found in this date range");
        if (exportControls) exportControls.style.display = "none";
        return;
      }

      const data = await Promise.all(
        orders.map(async (o) => {
          const id = o.orderId || o.id;

          let details = null;
          try {
            details = await window.HomemadeCookieApi.getAdminOrderDetails(id);
            // Debug: Log the structure to console
            console.log(`Order ${id} details:`, details);
            if (details?.items && details.items.length > 0) {
              console.log(`Order ${id} first item:`, details.items[0]);
            }
          } catch (err) {
            console.warn(`Could not fetch details for order ${id}:`, err);
          }

          // Process items to ensure proper names
          const rawItems = details?.items || [];
          const processedItems = rawItems.map(item => ({
            name: getItemName(item),
            quantity: getItemQuantity(item),
            price: item.price || item.unitPrice || item.Price || 0
          }));

          return {
            orderId: id,
            orderDate: (o.orderDate || "").split("T")[0],
            totalAmount: parseFloat(o.totalAmount || 0),
            customerEmail: details?.customerEmail || o.customerEmail || `User #${o.customerId}`,
            items: processedItems
          };
        })
      );

      localReportCache = data;

      let total = 0;

      if (tableBody) {
        tableBody.innerHTML = "";
        data.forEach(r => {
          total += r.totalAmount;

          // Create items display string with proper names
          const itemsDisplay = (r.items || [])
            .map(i => `${i.name} (x${i.quantity})`)
            .join(", ");

          const tr = document.createElement("tr");
          tr.innerHTML = `
            <td>#${r.orderId}</td>
            <td>${r.orderDate}</td>
            <td>${escapeHtml(r.customerEmail)}</td>
            <td>${escapeHtml(itemsDisplay || 'No items')}</td>
            <td>$${r.totalAmount.toFixed(2)}</td>
          `;
          tableBody.appendChild(tr);
        });
      }

      if (grossSumCell) {
        grossSumCell.textContent = `$${total.toFixed(2)}`;
      }

      if (exportControls) exportControls.style.display = "flex";
      if (previewContainer) previewContainer.style.display = "block";

      updateStatus(`Report loaded successfully - ${data.length} orders found`);

    } catch (err) {
      console.error("Error loading report:", err);
      updateStatus("Error loading report: " + (err.message || "Unknown error"), true);
    }
  }

  // Helper function to prevent XSS
  function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  // Event listeners
  if (btnFetch) {
    btnFetch.addEventListener("click", fetchReport);
  }

  // Export functionality
  if (btnExportCsv) {
    btnExportCsv.addEventListener("click", function() {
      if (!localReportCache.length) {
        updateStatus("No data to export", true);
        return;
      }
      exportToCSV(localReportCache);
    });
  }

  if (btnExportPdf) {
    btnExportPdf.addEventListener("click", function() {
      if (!localReportCache.length) {
        updateStatus("No data to export", true);
        return;
      }
      exportToPDF(localReportCache);
    });
  }

  function exportToCSV(data) {
    const headers = ['Order ID', 'Date', 'Customer Email', 'Items', 'Total Amount'];
    const rows = data.map(r => [
      r.orderId,
      r.orderDate,
      r.customerEmail,
      (r.items || []).map(i => `${i.name} (x${i.quantity})`).join('; '),
      r.totalAmount.toFixed(2)
    ]);
    
    const csvContent = [headers, ...rows]
      .map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
      .join('\n');
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.setAttribute('download', `sales_report_${new Date().toISOString().split('T')[0]}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    
    updateStatus("CSV exported successfully");
  }

  function exportToPDF(data) {
    updateStatus("Preparing PDF...");
    // Simple print-based PDF export
    setTimeout(() => {
      window.print();
    }, 500);
  }

  // Auto-load report when page loads
  fetchReport();
});