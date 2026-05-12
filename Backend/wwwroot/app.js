const apiBase = "/api";

const typeNames = {
  1: "Elektrik",
  2: "Su",
  3: "Internet",
  4: "GSM",
  5: "Diger"
};

const statusNames = {
  1: "Basarili",
  2: "Basarisiz"
};

let currentCustomer = JSON.parse(localStorage.getItem("customer") || "null");
let lastDebtBySubscription = {};

async function request(path, options) {
  const response = await fetch(`${apiBase}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "Islem basarisiz oldu.");
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
  document.querySelector("#authPanel").classList.toggle("hidden", isLoggedIn);
  document.querySelector("#appPanel").classList.toggle("hidden", !isLoggedIn);
  document.querySelector("#logoutButton").classList.toggle("hidden", !isLoggedIn);

  if (!isLoggedIn) {
    return;
  }

  document.querySelector("#profileName").value = currentCustomer.fullName;
  document.querySelector("#profileEmail").value = currentCustomer.email;
  document.querySelector("#profilePhone").value = currentCustomer.phoneNumber;
  document.querySelector("#profileUsername").value = currentCustomer.username;
  document.querySelector("#profilePassword").value = "";

  loadDashboard().catch(error => alert(error.message));
}

async function loadDashboard() {
  await Promise.all([loadSubscriptions(), loadPayments(), loadReminders()]);
}

async function loadSubscriptions() {
  const subscriptions = await request(`/subscriptions/customer/${currentCustomer.id}`);
  const container = document.querySelector("#subscriptions");

  if (subscriptions.length === 0) {
    container.innerHTML = `<p class="muted">Henuz abonelik yok.</p>`;
    return;
  }

  container.innerHTML = `<div class="list">${subscriptions.map(subscription => `
    <div class="row">
      <strong>${typeNames[subscription.type]} - ${subscription.providerName}</strong>
      <span class="muted">No: ${subscription.subscriberNumber} | Son odeme gunu: ${subscription.paymentDueDay}</span>
      <div id="debt-${subscription.id}" class="muted"></div>
      <div class="actions">
        <button onclick="queryDebt(${subscription.id})">Borc Sorgula</button>
        <button class="secondary" onclick="payDebt(${subscription.id})">Ode</button>
        <button class="danger" onclick="deleteSubscription(${subscription.id})">Sil</button>
      </div>
    </div>
  `).join("")}</div>`;
}

async function loadPayments() {
  const payments = await request(`/payments/customer/${currentCustomer.id}`);
  const container = document.querySelector("#payments");

  if (payments.length === 0) {
    container.innerHTML = `<p class="muted">Henuz odeme kaydi yok.</p>`;
    return;
  }

  container.innerHTML = `<div class="list">${payments.map(payment => `
    <div class="row">
      <strong>Abonelik Id: ${payment.subscriptionId} | ${payment.amount} TL</strong>
      <span class="muted">Donem: ${payment.period} | Durum: ${statusNames[payment.status]} | Tarih: ${new Date(payment.paymentDate).toLocaleString("tr-TR")}</span>
    </div>
  `).join("")}</div>`;
}

async function loadReminders() {
  const reminders = await request(`/reminders/customer/${currentCustomer.id}`);
  const container = document.querySelector("#reminders");

  if (reminders.length === 0) {
    container.innerHTML = "Bu ay odenmemis aktif abonelik yok.";
    return;
  }

  container.innerHTML = `<div class="list">${reminders.map(item => `
    <div class="row">
      <strong>${item.providerName}</strong>
      <span class="muted">No: ${item.subscriberNumber} | Donem: ${item.period} | Son odeme: ${item.dueDate}</span>
    </div>
  `).join("")}</div>`;
}

async function queryDebt(subscriptionId) {
  const debt = await request(`/debts/subscription/${subscriptionId}`);
  lastDebtBySubscription[subscriptionId] = debt;
  document.querySelector(`#debt-${subscriptionId}`).textContent = `Borc: ${debt.amount} TL | Son odeme: ${debt.dueDate} | Donem: ${debt.period}`;
}

async function payDebt(subscriptionId) {
  if (!lastDebtBySubscription[subscriptionId]) {
    await queryDebt(subscriptionId);
  }

  const debt = lastDebtBySubscription[subscriptionId];
  await request("/payments", {
    method: "POST",
    body: JSON.stringify({ subscriptionId, amount: debt.amount, period: debt.period })
  });

  await Promise.all([loadPayments(), loadReminders()]);
  alert("Odeme kaydi olusturuldu.");
}

async function deleteSubscription(id) {
  await request(`/subscriptions/${id}`, { method: "DELETE" });
  await Promise.all([loadSubscriptions(), loadPayments(), loadReminders()]);
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
  setCurrentCustomer(customer);
});

document.querySelector("#profileForm").addEventListener("submit", async event => {
  event.preventDefault();
  await request(`/customers/${currentCustomer.id}`, {
    method: "PUT",
    body: JSON.stringify({
      fullName: document.querySelector("#profileName").value,
      email: document.querySelector("#profileEmail").value,
      phoneNumber: document.querySelector("#profilePhone").value,
      username: document.querySelector("#profileUsername").value,
      password: document.querySelector("#profilePassword").value || null
    })
  });

  const updated = await request(`/customers/${currentCustomer.id}`);
  setCurrentCustomer(updated);
  alert("Profil guncellendi.");
});

document.querySelector("#subscriptionForm").addEventListener("submit", async event => {
  event.preventDefault();
  await request("/subscriptions", {
    method: "POST",
    body: JSON.stringify({
      customerId: currentCustomer.id,
      type: Number(document.querySelector("#subscriptionType").value),
      providerName: document.querySelector("#providerName").value,
      subscriberNumber: document.querySelector("#subscriberNumber").value,
      status: 1,
      paymentDueDay: Number(document.querySelector("#paymentDueDay").value)
    })
  });

  event.target.reset();
  document.querySelector("#paymentDueDay").value = 10;
  await Promise.all([loadSubscriptions(), loadReminders()]);
});

document.querySelector("#logoutButton").addEventListener("click", () => {
  localStorage.removeItem("customer");
  currentCustomer = null;
  lastDebtBySubscription = {};
  renderSession();
});

document.querySelector("#refreshSubscriptions").addEventListener("click", () => loadSubscriptions().catch(error => alert(error.message)));
document.querySelector("#refreshPayments").addEventListener("click", () => loadPayments().catch(error => alert(error.message)));
document.querySelector("#refreshReminders").addEventListener("click", () => loadReminders().catch(error => alert(error.message)));

renderSession();
