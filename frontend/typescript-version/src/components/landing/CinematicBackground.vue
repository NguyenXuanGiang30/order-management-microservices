<template>
  <div
    class="cinematic-background"
    data-testid="cinematic-background"
    aria-hidden="true"
  >
    <div
      class="cinematic-background__atmosphere cinematic-background__atmosphere--amber"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-background__atmosphere cinematic-background__atmosphere--red"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-background__atmosphere cinematic-background__atmosphere--violet"
      data-motion-layer="continuous"
    />

    <div
      class="cinematic-background__grid cinematic-background__grid--far"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-background__grid cinematic-background__grid--near"
      data-motion-layer="continuous"
    />

    <div
      class="cinematic-background__scan"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-background__sweep"
      data-motion-layer="continuous"
    />

    <div class="cinematic-background__signals">
      <span
        v-for="index in 20"
        :key="index"
        class="cinematic-background__signal"
        :style="{ '--signal-index': index }"
        data-motion-layer="continuous"
      />
    </div>

    <div
      class="cinematic-background__vignette"
      data-motion-layer="continuous"
    />
  </div>
</template>

<style scoped>
.cinematic-background {
  position: fixed;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  background:
    linear-gradient(145deg, rgb(5 5 8) 0%, rgb(14 10 13) 42%, rgb(7 7 12) 100%),
    #07080c;
  pointer-events: none;
}

.cinematic-background::before,
.cinematic-background::after {
  position: absolute;
  inset: 0;
  content: "";
  pointer-events: none;
}

.cinematic-background::before {
  background:
    radial-gradient(ellipse at 16% 18%, rgb(255 180 87 / 0.28), transparent 32%),
    radial-gradient(ellipse at 86% 10%, rgb(159 122 255 / 0.2), transparent 35%),
    radial-gradient(ellipse at 72% 82%, rgb(188 54 37 / 0.19), transparent 30%);
  opacity: 0.92;
}

.cinematic-background::after {
  background:
    linear-gradient(rgb(255 255 255 / 0.028) 1px, transparent 1px),
    linear-gradient(90deg, rgb(255 255 255 / 0.024) 1px, transparent 1px);
  background-size: 100% 3px, 3px 100%;
  mix-blend-mode: soft-light;
  opacity: 0.34;
}

.cinematic-background__atmosphere,
.cinematic-background__grid,
.cinematic-background__scan,
.cinematic-background__sweep,
.cinematic-background__signals,
.cinematic-background__vignette {
  position: absolute;
  inset: 0;
}

.cinematic-background__atmosphere {
  border-radius: 999px;
  filter: blur(46px);
  opacity: 0.82;
  will-change: transform;
}

.cinematic-background__atmosphere--amber {
  width: min(58vw, 760px);
  height: min(44vw, 560px);
  inset-block-start: -16%;
  inset-inline-end: 2%;
  background: linear-gradient(135deg, rgb(255 180 87 / 0.28), rgb(188 54 37 / 0.12));
  animation: cinematicFieldDrift 18s cubic-bezier(0.22, 1, 0.36, 1) infinite;
}

.cinematic-background__atmosphere--red {
  width: min(46vw, 620px);
  height: min(34vw, 460px);
  inset-block-end: 8%;
  inset-inline-end: -16%;
  background: rgb(188 54 37 / 0.2);
  animation: cinematicFieldDrift 22s cubic-bezier(0.22, 1, 0.36, 1) infinite reverse;
}

.cinematic-background__atmosphere--violet {
  width: min(48vw, 620px);
  height: min(48vw, 620px);
  inset-block-end: -22%;
  inset-inline-start: -12%;
  background: rgb(159 122 255 / 0.18);
  animation: cinematicFieldPulse 20s ease-in-out infinite;
}

.cinematic-background__grid {
  background:
    linear-gradient(rgb(255 180 87 / 0.08) 1px, transparent 1px),
    linear-gradient(90deg, rgb(159 122 255 / 0.055) 1px, transparent 1px);
  mask-image: linear-gradient(to bottom, transparent 0%, black 18%, black 78%, transparent 100%);
  transform-origin: top center;
}

.cinematic-background__grid--far {
  background-size: 112px 112px;
  opacity: 0.28;
  transform: perspective(1100px) rotateX(61deg) translateY(-31%);
  animation: cinematicGridDriftFar 22s linear infinite;
}

.cinematic-background__grid--near {
  background-size: 48px 48px;
  opacity: 0.22;
  transform: perspective(900px) rotateX(58deg) translateY(16%);
  animation: cinematicGridDriftNear 12s linear infinite;
}

.cinematic-background__scan {
  background:
    linear-gradient(90deg, transparent, rgb(255 180 87 / 0.12), transparent),
    linear-gradient(to bottom, transparent 0%, rgb(255 255 255 / 0.06) 50%, transparent 100%);
  mix-blend-mode: screen;
  opacity: 0.52;
  transform: translateY(-40%);
  animation: cinematicScan 7s cubic-bezier(0.22, 1, 0.36, 1) infinite;
}

.cinematic-background__sweep {
  background: linear-gradient(116deg, transparent 27%, rgb(255 180 87 / 0.18) 43%, rgb(159 122 255 / 0.1) 50%, transparent 62%);
  mix-blend-mode: screen;
  opacity: 0;
  transform: translateX(-68%) skewX(-10deg);
  animation: cinematicLightSweep 9s cubic-bezier(0.16, 1, 0.3, 1) infinite;
}

.cinematic-background__signals {
  overflow: hidden;
  mask-image: linear-gradient(to bottom, transparent 0%, black 14%, black 82%, transparent 100%);
}

.cinematic-background__signal {
  position: absolute;
  width: clamp(64px, 9vw, 150px);
  height: 1px;
  inset-block-start: calc(4% + (var(--signal-index) * 4.7%));
  inset-inline-start: calc(-18% + (var(--signal-index) * 5.8%));
  background: linear-gradient(90deg, transparent, rgb(255 180 87 / 0.95), rgb(188 54 37 / 0.4), transparent);
  box-shadow: 0 0 18px rgb(255 180 87 / 0.45);
  opacity: 0;
  transform: rotate(-18deg) translate3d(-16vw, 0, 0);
  animation: cinematicSignalRun 8s linear infinite;
  animation-delay: calc(var(--signal-index) * -360ms);
  will-change: transform, opacity;
}

.cinematic-background__signal:nth-child(3n) {
  background: linear-gradient(90deg, transparent, rgb(159 122 255 / 0.88), rgb(255 180 87 / 0.36), transparent);
  box-shadow: 0 0 18px rgb(159 122 255 / 0.34);
}

.cinematic-background__signal:nth-child(4n) {
  width: clamp(28px, 5vw, 84px);
  background: linear-gradient(90deg, transparent, rgb(255 95 75 / 0.74), transparent);
}

.cinematic-background__vignette {
  background:
    radial-gradient(ellipse at center, transparent 36%, rgb(0 0 0 / 0.38) 82%),
    linear-gradient(to bottom, rgb(0 0 0 / 0.34), transparent 28%, rgb(0 0 0 / 0.44));
  opacity: 0.96;
  animation: cinematicVignetteBreath 14s ease-in-out infinite;
}

@keyframes cinematicFieldDrift {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }

  50% {
    transform: translate3d(-4%, 3%, 0) scale(1.08);
  }
}

@keyframes cinematicFieldPulse {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
    opacity: 0.62;
  }

  50% {
    transform: translate3d(6%, -4%, 0) scale(1.12);
    opacity: 0.88;
  }
}

@keyframes cinematicGridDriftFar {
  from {
    background-position: 0 0, 0 0;
  }

  to {
    background-position: 0 112px, 112px 0;
  }
}

@keyframes cinematicGridDriftNear {
  from {
    background-position: 0 0, 0 0;
  }

  to {
    background-position: 0 48px, 48px 0;
  }
}

@keyframes cinematicScan {
  0% {
    opacity: 0;
    transform: translateY(-48%);
  }

  18% {
    opacity: 0.58;
  }

  48% {
    opacity: 0.2;
  }

  100% {
    opacity: 0;
    transform: translateY(58%);
  }
}

@keyframes cinematicLightSweep {
  0% {
    opacity: 0;
    transform: translateX(-68%) skewX(-10deg);
  }

  20% {
    opacity: 0.82;
  }

  54% {
    opacity: 0.28;
  }

  100% {
    opacity: 0;
    transform: translateX(76%) skewX(-10deg);
  }
}

@keyframes cinematicSignalRun {
  0% {
    opacity: 0;
    transform: rotate(-18deg) translate3d(-18vw, 0, 0);
  }

  12% {
    opacity: 0.88;
  }

  70% {
    opacity: 0.36;
  }

  100% {
    opacity: 0;
    transform: rotate(-18deg) translate3d(86vw, -30vh, 0);
  }
}

@keyframes cinematicVignetteBreath {
  0%,
  100% {
    opacity: 0.92;
  }

  50% {
    opacity: 1;
  }
}

@media (max-width: 760px) {
  .cinematic-background::before {
    background:
      radial-gradient(ellipse at 20% 12%, rgb(255 180 87 / 0.24), transparent 36%),
      radial-gradient(ellipse at 86% 24%, rgb(159 122 255 / 0.18), transparent 38%),
      radial-gradient(ellipse at 62% 92%, rgb(188 54 37 / 0.18), transparent 34%);
  }

  .cinematic-background__grid--far {
    background-size: 84px 84px;
    transform: perspective(850px) rotateX(60deg) translateY(-22%);
  }

  .cinematic-background__grid--near {
    background-size: 40px 40px;
    opacity: 0.16;
  }

  .cinematic-background__signal {
    width: clamp(44px, 20vw, 92px);
  }
}

@media (prefers-reduced-motion: reduce) {
  [data-motion-layer="continuous"] {
    animation: none !important;
    transition-duration: 0s !important;
  }

  .cinematic-background__atmosphere {
    opacity: 0.58;
  }

  .cinematic-background__scan,
  .cinematic-background__sweep,
  .cinematic-background__signal {
    opacity: 0.18;
  }
}
</style>
