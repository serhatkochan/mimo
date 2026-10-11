const mascots = [
  { id: "bee", name: "Arı" },
  { id: "cactus", name: "Kaktüs" },
  { id: "jellyfish", name: "Jeli" },
  { id: "ghost", name: "Hayalet" },
  { id: "snail", name: "Salyangoz" },
  { id: "capybara", name: "Kapibara" },
  { id: "mushroom", name: "Mantar" },
  { id: "crab", name: "Yengeç" },
  { id: "seal", name: "Fok" },
  { id: "teapot", name: "Demlik" },
];

const stageNames = {
  focus: "Odak zamanı",
  short: "Kısa mola",
  long: "Uzun mola",
};

const previewImage = document.querySelector("#preview-image");
const previewSprite = document.querySelector("#preview-sprite");
const previewName = document.querySelector("#preview-name");
const previewCount = document.querySelector("#preview-count");
const previewState = document.querySelector("#preview-state");
const mascotButtons = [...document.querySelectorAll("[data-mascot]")];
const stageButtons = [...document.querySelectorAll(".stage-tabs button")];

mascotButtons.forEach((button) => {
  button.addEventListener("click", () => {
    const index = mascots.findIndex((mascot) => mascot.id === button.dataset.mascot);
    if (index === -1) return;

    const mascot = mascots[index];
    previewImage.src = `assets/mascots/${mascot.id}.png`;
    previewName.textContent = mascot.name;
    previewCount.textContent = `${String(index + 1).padStart(2, "0")} / 10`;

    mascotButtons.forEach((item) => {
      const selected = item === button;
      item.classList.toggle("selected", selected);
      item.setAttribute("aria-pressed", String(selected));
    });
  });
});

stageButtons.forEach((button) => {
  button.addEventListener("click", () => {
    const stage = button.dataset.stage;
    if (!Object.hasOwn(stageNames, stage)) return;

    previewSprite.dataset.stage = stage;
    previewState.textContent = stageNames[stage];

    stageButtons.forEach((item) => {
      const selected = item === button;
      item.classList.toggle("active", selected);
      item.setAttribute("aria-pressed", String(selected));
    });
  });
});

const heroArt = document.querySelector(".hero-art");
const demoSprite = document.querySelector("#demo-sprite");
const demoPhase = document.querySelector("#demo-phase");
const demoTime = document.querySelector("#demo-time");
const demoPlay = document.querySelector("#demo-play");
const demoNext = document.querySelector("#demo-next");
const demoReset = document.querySelector("#demo-reset");
const demoDurations = { focus: 25 * 60_000, short: 5 * 60_000, long: 15 * 60_000 };
const demo = {
  phase: "focus",
  finishedFocusRounds: 0,
  remainingMs: demoDurations.focus,
  deadline: 0,
  running: false,
  touched: false,
};
let demoTicker = null;

function renderDemo() {
  const label = demo.phase === "focus"
    ? `${demo.finishedFocusRounds % 4 + 1}. TUR`
    : demo.phase === "short" ? "KISA MOLA" : "UZUN MOLA";
  const seconds = Math.ceil(demo.remainingMs / 1000);
  const time = `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
  if (demoPhase.textContent !== label) demoPhase.textContent = label;
  if (demoTime.textContent !== time) demoTime.textContent = time;

  heroArt.dataset.phase = demo.phase;
  heroArt.classList.toggle("is-running", demo.running);
  heroArt.classList.toggle("is-previewing", !demo.touched);
  heroArt.classList.toggle("is-paused", demo.touched && !demo.running);
  demoSprite.dataset.stage = demo.phase;
  const playLabel = demo.running ? "Duraklat"
    : demo.remainingMs < demoDurations[demo.phase] ? "Devam et" : "Başlat";
  demoPlay.setAttribute("aria-label", playLabel);
  demoPlay.title = playLabel;
}

function syncDemoTicker() {
  if (demo.running && demoTicker === null) {
    demoTicker = window.setInterval(updateDemoClock, 250);
  } else if (!demo.running && demoTicker !== null) {
    window.clearInterval(demoTicker);
    demoTicker = null;
  }
}

function advanceDemoPhase(now) {
  if (demo.phase === "focus") {
    demo.finishedFocusRounds++;
    demo.phase = demo.finishedFocusRounds % 4 === 0 ? "long" : "short";
  } else {
    demo.phase = "focus";
  }
  demo.remainingMs = demoDurations[demo.phase];
  demo.deadline = demo.running ? now + demo.remainingMs : 0;
}

function updateDemoClock() {
  if (!demo.running) return;
  const now = Date.now();
  demo.remainingMs = Math.max(0, demo.deadline - now);
  if (demo.remainingMs === 0) {
    demo.running = false;
    advanceDemoPhase(now);
    syncDemoTicker();
  }
  renderDemo();
}

demoPlay.addEventListener("click", () => {
  demo.touched = true;
  if (demo.running) {
    updateDemoClock();
    demo.running = false;
    demo.deadline = 0;
  } else {
    demo.running = true;
    demo.deadline = Date.now() + demo.remainingMs;
  }
  syncDemoTicker();
  renderDemo();
});

demoNext.addEventListener("click", () => {
  demo.touched = true;
  advanceDemoPhase(Date.now());
  renderDemo();
});

demoReset.addEventListener("click", () => {
  demo.touched = true;
  demo.running = false;
  demo.deadline = 0;
  demo.remainingMs = demoDurations[demo.phase];
  syncDemoTicker();
  renderDemo();
});

renderDemo();
