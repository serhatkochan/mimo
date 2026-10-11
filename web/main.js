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
