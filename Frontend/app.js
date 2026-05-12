const apiBase = "/api";

const typeNames = {
  1: "Elektrik",
  2: "Su",
  3: "İnternet",
  4: "GSM",
  5: "Diğer"
};

const statusNames = {
  1: "Başarılı",
  2: "Başarısız"
};

let currentCustomer = JSON.parse(localStorage.getItem("customer") || "null");
let lastDebtBySubscription = {};
let currentPage = "home";
let validatedSubscription = null;
let appDateInfo = null;
let subscriptionItems = [];
let subscriptionPayments = [];
let paymentItems = [];
let selectedSubscriptionId = null;

async function request(path, options) {
  const headers = { "Content-Type": "application/json", ...(options?.headers || {}) };
  if (currentCustomer?.isAdmin) {
    headers["X-Admin-User-Id"] = currentCustomer.id;
  }

  const response = await fetch(`${apiBase}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "İşlem başarısız oldu.");
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function setCurrentCustomer(customer) {
  currentCustomer = customer;
  localStorage.setItem("customer", JSON.stringify(customer));
  renderSession();
}

function renderSession() {
  const isLoggedIn = Boolean(currentCustomer);
  const isAdmin = Boolean(currentCustomer?.isAdmin);
  document.querySelector("#authPanel").classList.toggle("hidden", isLoggedIn);
  document.querySelector("#appPanel").classList.toggle("hidden", !isLoggedIn);
  document.querySelector("#mainNav").classList.toggle("hidden", !isLoggedIn);
  document.querySelector("#customerInfo").classList.toggle("hidden", !isLoggedIn);
  document.querySelector("#adminControls").classList.toggle("hidden", !isAdmin);

  if (!isLoggedIn) {
    return;
  }

  renderCustomerInfo();
  refreshAppDate().then(() => showPage(currentPage)).catch(error => alert(error.message));
}

function renderCustomerInfo() {
  document.querySelector("#customerInfo").textContent = `${currentCustomer.fullName} | ${currentCustomer.username} | ${currentCustomer.email}`;
}

function showPage(pageName) {
  currentPage = pageName;
  document.querySelectorAll(".page-section").forEach(section => section.classList.add("hidden"));
  document.querySelector(`#${pageName}Page`).classList.remove("hidden");
  document.querySelectorAll(".nav-button").forEach(button => button.classList.remove("active"));

  const activeButton = document.querySelector(`#${pageName}Nav`);
  if (activeButton) {
    activeButton.classList.add("active");
  }

  if (pageName === "home") {
    loadDashboard().catch(error => alert(error.message));
  }

  if (pageName === "profile") {
    fillProfileForm();
  }

  if (pageName === "addSubscription") {
    resetSubscriptionValidation();
  }

  if (pageName === "subscriptionDetail") {
    renderSubscriptionDetail();
  }

}

async function refreshAppDate() {
  appDateInfo = await request("/test-date");
  renderDateBanner();
}

function getActiveDate() {
  const source = appDateInfo?.activeDate || new Date().toISOString().slice(0, 10);
  return new Date(`${source}T00:00:00`);
}

function renderDateBanner() {
  renderTestDateInfo();
}

function renderTestDateInfo() {
  const info = document.querySelector("#testDateInfo");
  const input = document.querySelector("#testDateInput");
  if (!appDateInfo || !info || !input) {
    return;
  }

  info.textContent = appDateInfo.isTestMode
    ? `Aktif tarih: ${appDateInfo.activeDate} | Gerçek tarih: ${appDateInfo.realDate}`
    : `Gerçek tarih kullanılıyor: ${appDateInfo.realDate}`;
  input.value = appDateInfo.activeDate;
}

function fillProfileForm() {
  document.querySelector("#profileInfo").innerHTML = `
    <div class="profile-row"><strong>Ad Soyad</strong><span>${currentCustomer.fullName}</span></div>
    <div class="profile-row"><strong>Email</strong><span>${currentCustomer.email}</span></div>
    <div class="profile-row"><strong>Telefon</strong><span>${currentCustomer.phoneNumber}</span></div>
    <div class="profile-row"><strong>Kullanıcı adı</strong><span>${currentCustomer.username}</span></div>
  `;
}

async function loadDashboard() {
  await Promise.all([loadSubscriptions(), loadPayments(), loadReminders()]);
}

async function loadSubscriptions() {
  subscriptionItems = await request(`/subscriptions/customer/${currentCustomer.id}`);
  subscriptionPayments = await request(`/payments/customer/${currentCustomer.id}`);
  renderSubscriptions();
}

function renderSubscriptions() {
  const container = document.querySelector("#subscriptions");

  if (subscriptionItems.length === 0) {
    container.innerHTML = `<p class="muted">Henüz abonelik yok.</p>`;
    return;
  }

  const search = document.querySelector("#subscriptionSearch").value.trim().toLowerCase();
  const sort = document.querySelector("#subscriptionSort").value;
  const items = subscriptionItems
    .map(subscription => ({ subscription, status: getSubscriptionStatus(subscription, subscriptionPayments) }))
    .filter(item => getSubscriptionSearchText(item.subscription, item.status).includes(search))
    .sort((a, b) => compareSubscriptions(a, b, sort));

  if (items.length === 0) {
    container.innerHTML = `<p class="muted">Aramaya uygun abonelik bulunamadı.</p>`;
    return;
  }

  container.innerHTML = `<div class="list">${items.map(item => {
    const subscription = item.subscription;
    const status = item.status;

    return `
    <div class="row ${status.className}">
      <strong>${typeNames[subscription.type]} - ${subscription.providerName}</strong>
      <span class="muted">No: ${subscription.subscriberNumber} | Durum: ${subscription.status === 1 ? "Aktif" : "Pasif"} | Fatura kesim günü: ${subscription.billingDay} | Ödeme tercihi: Her ayın ${subscription.preferredPaymentDay}. günü</span>
      <span class="status-label">${status.label}</span>
      <div class="actions">
        <button onclick="openSubscriptionDetail(${subscription.id})">Detay</button>
      </div>
    </div>
  `;
  }).join("")}</div>`;
}

function getSubscriptionSearchText(subscription, status) {
  return [
    typeNames[subscription.type],
    subscription.providerName,
    subscription.subscriberNumber,
    status.label,
    subscription.billingDay,
    subscription.preferredPaymentDay
  ].join(" ").toLowerCase();
}

function compareSubscriptions(a, b, sort) {
  if (sort === "paid") {
    return (a.status.rank === 4 ? 0 : 1) - (b.status.rank === 4 ? 0 : 1) || a.subscription.providerName.localeCompare(b.subscription.providerName);
  }

  if (sort === "provider") {
    return a.subscription.providerName.localeCompare(b.subscription.providerName);
  }

  if (sort === "preferredDay") {
    return a.subscription.preferredPaymentDay - b.subscription.preferredPaymentDay;
  }

  if (sort === "billingDay") {
    return a.subscription.billingDay - b.subscription.billingDay;
  }

  return a.status.rank - b.status.rank || a.subscription.preferredPaymentDay - b.subscription.preferredPaymentDay;
}

function getSubscriptionStatus(subscription, payments) {
  const today = getActiveDate();
  const period = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}`;
  const paidThisMonth = payments.some(payment =>
    payment.subscriptionId === subscription.id && payment.period === period && payment.status === 1);

  if (subscription.status !== 1) {
    return { className: "subscription-passive", label: "Pasif abonelik", rank: 5 };
  }

  if (paidThisMonth) {
    return { className: "subscription-paid", label: "Bu ay ödendi", rank: 4 };
  }

  const dueDate = new Date(today.getFullYear(), today.getMonth(), subscription.preferredPaymentDay);
  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const daysUntilPayment = Math.ceil((dueDate - startOfToday) / 86400000);

  if (daysUntilPayment <= 0) {
    return { className: "subscription-overdue", label: daysUntilPayment === 0 ? "Ödeme günü bugün" : "Ödeme günü geçti", rank: 1 };
  }

  if (daysUntilPayment <= 7) {
    return { className: "subscription-due-soon", label: `Ödeme gününe ${daysUntilPayment} gün kaldı`, rank: 2 };
  }

  return { className: "", label: "Ödeme tarihi bekleniyor", rank: 3 };
}

async function loadPayments() {
  paymentItems = await request(`/payments/customer/${currentCustomer.id}`);
  renderPayments();
}

function renderPayments() {
  const container = document.querySelector("#payments");

  if (paymentItems.length === 0) {
    container.innerHTML = `<p class="muted">Henüz ödeme kaydı yok.</p>`;
    return;
  }

  const search = document.querySelector("#paymentSearch").value.trim().toLowerCase();
  const sort = document.querySelector("#paymentSort").value;
  const items = paymentItems
    .filter(payment => getPaymentSearchText(payment).includes(search))
    .sort((a, b) => comparePayments(a, b, sort));

  if (items.length === 0) {
    container.innerHTML = `<p class="muted">Aramaya uygun ödeme kaydı bulunamadı.</p>`;
    return;
  }

  container.innerHTML = `<div class="list">${items.map(payment => `
    <div class="row">
      <strong>${typeNames[payment.subscriptionType]} - ${payment.providerName} | ${payment.amount} TL</strong>
      <span class="muted">Abonelik No: ${payment.subscriberNumber}</span>
      <span class="muted">Dönem: ${payment.period} | Durum: ${statusNames[payment.status]} | Tarih: ${new Date(payment.paymentDate).toLocaleString("tr-TR")}</span>
    </div>
  `).join("")}</div>`;
}

function getPaymentSearchText(payment) {
  return [
    typeNames[payment.subscriptionType],
    payment.providerName,
    payment.subscriberNumber,
    payment.period,
    statusNames[payment.status],
    payment.amount
  ].join(" ").toLowerCase();
}

function comparePayments(a, b, sort) {
  if (sort === "oldest") {
    return new Date(a.paymentDate) - new Date(b.paymentDate);
  }

  if (sort === "amountDesc") {
    return b.amount - a.amount;
  }

  if (sort === "amountAsc") {
    return a.amount - b.amount;
  }

  if (sort === "successful") {
    return (a.status === 1 ? 0 : 1) - (b.status === 1 ? 0 : 1) || new Date(b.paymentDate) - new Date(a.paymentDate);
  }

  if (sort === "failed") {
    return (a.status === 2 ? 0 : 1) - (b.status === 2 ? 0 : 1) || new Date(b.paymentDate) - new Date(a.paymentDate);
  }

  return new Date(b.paymentDate) - new Date(a.paymentDate);
}

async function loadReminders() {
  const reminders = await request(`/reminders/customer/${currentCustomer.id}`);
  const container = document.querySelector("#reminders");

  if (reminders.length === 0) {
    container.innerHTML = "Yaklaşan veya geçmiş ödeme bildirimi yok.";
    return;
  }

  container.innerHTML = `<div class="list">${reminders.map(item => `
    <div class="row">
      <strong>${item.providerName}</strong>
      <span class="muted">${getReminderMessage(item)} Dönem: ${item.period} | Planlanan ödeme günü: ${item.dueDate}</span>
    </div>
  `).join("")}</div>`;
}

function getReminderMessage(item) {
  if (item.daysUntilPayment < 0) {
    return `Ödeme günü ${Math.abs(item.daysUntilPayment)} gün geçti.`;
  }

  if (item.daysUntilPayment === 0) {
    return "Ödeme günü bugün.";
  }

  return `Ödeme gününe ${item.daysUntilPayment} gün kaldı.`;
}

async function queryDebt(subscriptionId) {
  const debt = await request(`/debts/subscription/${subscriptionId}`);
  const target = document.querySelector(`#debt-${subscriptionId}`);

  if (!debt.hasDebt) {
    delete lastDebtBySubscription[subscriptionId];
    if (target) {
      target.textContent = debt.message;
    }
    return debt;
  }

  lastDebtBySubscription[subscriptionId] = debt;
  if (target) {
    target.textContent = `Borç: ${debt.amount} TL | Fatura kesim günü: ${debt.dueDate} | Dönem: ${debt.period}`;
  }
  return debt;
}

async function payDebt(subscriptionId) {
  const debt = lastDebtBySubscription[subscriptionId] || await queryDebt(subscriptionId);

  if (!debt.hasDebt) {
    alert(debt.message);
    return;
  }

  const paymentResult = await request("/payments", {
    method: "POST",
    body: JSON.stringify({ subscriptionId, amount: debt.amount, period: debt.period })
  });

  delete lastDebtBySubscription[subscriptionId];
  await Promise.all([loadSubscriptions(), loadPayments(), loadReminders()]);
  if (currentPage === "subscriptionDetail") {
    renderSubscriptionDetail();
  }
  alert(paymentResult.message);
}

async function deleteSubscription(id) {
  if (!confirm("Bu aboneliği silmek istediğinize emin misiniz?")) {
    return;
  }

  await request(`/subscriptions/${id}`, { method: "DELETE" });
  selectedSubscriptionId = null;
  showPage("home");
  await Promise.all([loadSubscriptions(), loadPayments(), loadReminders()]);
}

function openSubscriptionDetail(subscriptionId) {
  selectedSubscriptionId = subscriptionId;
  showPage("subscriptionDetail");
}

function getSelectedSubscription() {
  return subscriptionItems.find(subscription => subscription.id === selectedSubscriptionId) || null;
}

function renderSubscriptionDetail() {
  const subscription = getSelectedSubscription();
  const detail = document.querySelector("#subscriptionDetail");
  const history = document.querySelector("#subscriptionPaymentHistory");

  if (!subscription) {
    detail.innerHTML = `<p class="muted">Abonelik bulunamadı.</p>`;
    history.innerHTML = "";
    return;
  }

  const status = getSubscriptionStatus(subscription, subscriptionPayments);
  document.querySelector("#editSubscriptionType").value = subscription.type;
  document.querySelector("#editProviderName").value = subscription.providerName;
  document.querySelector("#editSubscriberNumber").value = subscription.subscriberNumber;
  document.querySelector("#editPreferredPaymentDay").value = subscription.preferredPaymentDay;

  detail.innerHTML = `
    <div class="row ${status.className}">
      <strong>${typeNames[subscription.type]} - ${subscription.providerName}</strong>
      <span class="muted">Abonelik No: ${subscription.subscriberNumber}</span>
      <span class="muted">Durum: ${subscription.status === 1 ? "Aktif" : "Pasif"}</span>
      <span class="muted">Fatura kesim günü: Her ayın ${subscription.billingDay}. günü</span>
      <span class="muted">Ödeme tercihi: Her ayın ${subscription.preferredPaymentDay}. günü</span>
      <span class="status-label">${status.label}</span>
      <div id="debt-${subscription.id}" class="muted"></div>
      <div class="actions">
        <button ${subscription.status !== 1 ? "disabled" : ""} onclick="queryDebt(${subscription.id})">Borç Sorgula</button>
        <button ${subscription.status !== 1 ? "disabled" : ""} class="secondary" onclick="payDebt(${subscription.id})">Öde</button>
        <button class="secondary" onclick="toggleSubscriptionStatus(${subscription.id}, ${subscription.status === 1 ? 2 : 1})">${subscription.status === 1 ? "Pasife Al" : "Aktif Et"}</button>
        <button class="danger" onclick="deleteSubscription(${subscription.id})">Sil</button>
      </div>
    </div>
  `;

  const payments = paymentItems.filter(payment => payment.subscriptionId === subscription.id);
  history.innerHTML = payments.length === 0
    ? `<p class="muted">Bu aboneliğe ait ödeme kaydı yok.</p>`
    : `<div class="list">${payments.map(payment => `
      <div class="row">
        <strong>${payment.amount} TL</strong>
        <span class="muted">Dönem: ${payment.period} | Durum: ${statusNames[payment.status]} | Tarih: ${new Date(payment.paymentDate).toLocaleString("tr-TR")}</span>
      </div>
    `).join("")}</div>`;
}

async function toggleSubscriptionStatus(subscriptionId, status) {
  await request(`/subscriptions/${subscriptionId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status })
  });

  await Promise.all([loadSubscriptions(), loadReminders()]);
  renderSubscriptionDetail();
}

function getSubscriptionFormValues() {
  return {
    type: Number(document.querySelector("#subscriptionType").value),
    providerName: document.querySelector("#providerName").value,
    subscriberNumber: document.querySelector("#subscriberNumber").value
  };
}

function resetSubscriptionValidation() {
  validatedSubscription = null;
  document.querySelector("#subscriptionValidationResult").textContent = "";
  document.querySelector("#paymentPreferenceFields").classList.add("hidden");
}

async function validateSubscription() {
  const requestBody = getSubscriptionFormValues();
  const result = await request("/subscriptions/validate", {
    method: "POST",
    body: JSON.stringify(requestBody)
  });

  const resultContainer = document.querySelector("#subscriptionValidationResult");
  if (!result.isValid) {
    validatedSubscription = null;
    resultContainer.textContent = result.message;
    document.querySelector("#paymentPreferenceFields").classList.add("hidden");
    return;
  }

  validatedSubscription = { ...requestBody, billingDay: result.billingDay };
  resultContainer.textContent = `${result.message} Fatura kesim günü: Her ayın ${result.billingDay}. günü.`;
  document.querySelector("#paymentPreferenceFields").classList.remove("hidden");
}

document.querySelector("#loginForm").addEventListener("submit", async event => {
  event.preventDefault();
  const customer = await request("/customers/login", {
    method: "POST",
    body: JSON.stringify({
      username: document.querySelector("#loginUsername").value,
      password: document.querySelector("#loginPassword").value
    })
  });

  event.target.reset();
  currentPage = "home";
  setCurrentCustomer(customer);
});

document.querySelector("#registerForm").addEventListener("submit", async event => {
  event.preventDefault();
  const customer = await request("/customers/register", {
    method: "POST",
    body: JSON.stringify({
      fullName: document.querySelector("#registerName").value,
      email: document.querySelector("#registerEmail").value,
      phoneNumber: document.querySelector("#registerPhone").value,
      username: document.querySelector("#registerUsername").value,
      password: document.querySelector("#registerPassword").value
    })
  });

  event.target.reset();
  currentPage = "home";
  setCurrentCustomer(customer);
});

document.querySelector("#deleteAccountButton").addEventListener("click", async () => {
  if (!confirm("Hesabınızı silmek istediğinize emin misiniz? Abonelik ve ödeme kayıtları korunarak hesabınız pasife alınır.")) {
    return;
  }

  await request(`/customers/${currentCustomer.id}`, { method: "DELETE" });
  localStorage.removeItem("customer");
  currentCustomer = null;
  lastDebtBySubscription = {};
  selectedSubscriptionId = null;
  currentPage = "home";
  renderSession();
});

document.querySelector("#editSubscriptionForm").addEventListener("submit", async event => {
  event.preventDefault();
  const subscription = getSelectedSubscription();
  if (!subscription) {
    return;
  }

  await request(`/subscriptions/${subscription.id}`, {
    method: "PUT",
    body: JSON.stringify({
      type: Number(document.querySelector("#editSubscriptionType").value),
      providerName: document.querySelector("#editProviderName").value,
      subscriberNumber: document.querySelector("#editSubscriberNumber").value,
      status: subscription.status,
      preferredPaymentDay: Number(document.querySelector("#editPreferredPaymentDay").value)
    })
  });

  await Promise.all([loadSubscriptions(), loadReminders()]);
  renderSubscriptionDetail();
  alert("Abonelik güncellendi.");
});

document.querySelector("#subscriptionForm").addEventListener("submit", async event => {
  event.preventDefault();
  const currentFormValues = getSubscriptionFormValues();

  if (!validatedSubscription ||
      validatedSubscription.type !== currentFormValues.type ||
      validatedSubscription.providerName !== currentFormValues.providerName ||
      validatedSubscription.subscriberNumber !== currentFormValues.subscriberNumber) {
    alert("Önce aboneliği kontrol edin.");
    return;
  }

  await request("/subscriptions", {
    method: "POST",
    body: JSON.stringify({
      customerId: currentCustomer.id,
      type: validatedSubscription.type,
      providerName: validatedSubscription.providerName,
      subscriberNumber: validatedSubscription.subscriberNumber,
      status: 1,
      preferredPaymentDay: Number(document.querySelector("#preferredPaymentDay").value)
    })
  });

  event.target.reset();
  document.querySelector("#preferredPaymentDay").value = 10;
  resetSubscriptionValidation();
  showPage("home");
  await Promise.all([loadSubscriptions(), loadReminders()]);
});

document.querySelector("#validateSubscriptionButton").addEventListener("click", () => validateSubscription().catch(error => alert(error.message)));
document.querySelector("#subscriptionType").addEventListener("change", resetSubscriptionValidation);
document.querySelector("#providerName").addEventListener("input", resetSubscriptionValidation);
document.querySelector("#subscriberNumber").addEventListener("input", resetSubscriptionValidation);

document.querySelector("#homeNav").addEventListener("click", () => showPage("home"));
document.querySelector("#profileNav").addEventListener("click", () => showPage("profile"));
document.querySelector("#addSubscriptionNav").addEventListener("click", () => showPage("addSubscription"));
document.querySelector("#backToHomeButton").addEventListener("click", () => showPage("home"));

document.querySelector("#testDateForm").addEventListener("submit", async event => {
  event.preventDefault();
  appDateInfo = await request("/test-date", {
    method: "POST",
    body: JSON.stringify({ date: document.querySelector("#testDateInput").value })
  });
  renderDateBanner();
  await loadDashboard();
});

document.querySelector("#resetTestDateButton").addEventListener("click", async () => {
  appDateInfo = await request("/test-date", { method: "DELETE" });
  renderDateBanner();
  await loadDashboard();
});

document.querySelector("#hardDeleteCustomerForm").addEventListener("submit", async event => {
  event.preventDefault();
  const customerId = Number(document.querySelector("#hardDeleteCustomerId").value);
  if (!confirm(`${customerId} ID'li müşteri kalıcı olarak silinsin mi? Bu işlem abonelik ve ödeme kayıtlarını da siler.`)) {
    return;
  }

  await request(`/admin/customers/${customerId}/hard-delete`, { method: "DELETE" });
  event.target.reset();

  if (currentCustomer?.id === customerId) {
    localStorage.removeItem("customer");
    currentCustomer = null;
    lastDebtBySubscription = {};
    selectedSubscriptionId = null;
    currentPage = "home";
    renderSession();
  }

  alert("Müşteri kalıcı olarak silindi.");
});

document.querySelector("#logoutButton").addEventListener("click", () => {
  localStorage.removeItem("customer");
  currentCustomer = null;
  lastDebtBySubscription = {};
  currentPage = "home";
  renderSession();
});

document.querySelector("#refreshSubscriptions").addEventListener("click", () => loadSubscriptions().catch(error => alert(error.message)));
document.querySelector("#refreshPayments").addEventListener("click", () => loadPayments().catch(error => alert(error.message)));
document.querySelector("#refreshReminders").addEventListener("click", () => loadReminders().catch(error => alert(error.message)));
document.querySelector("#subscriptionSearch").addEventListener("input", renderSubscriptions);
document.querySelector("#subscriptionSort").addEventListener("change", renderSubscriptions);
document.querySelector("#paymentSearch").addEventListener("input", renderPayments);
document.querySelector("#paymentSort").addEventListener("change", renderPayments);

renderSession();
