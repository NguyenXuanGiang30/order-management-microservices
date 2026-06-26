# RetailOps Cinematic Landing Redesign Design

Date: 2026-06-26
Status: Approved design direction, pending implementation plan

## Summary

Rebuild the public RetailOps landing page from scratch as a cinematic product showcase. The page should feel premium, dramatic, and immediately professional, closer to a 4K product trailer than a standard SaaS template. The authenticated app remains a restrained operational product UI; the cinematic treatment belongs to the public `/` route.

## Goals

- Make the first viewport feel high-end and memorable within seconds.
- Replace the current white/blue landing direction with a dark cinematic palette.
- Show RetailOps as a serious retail operations command center backed by microservices.
- Use motion beyond cards and popups: the background itself should feel alive.
- Preserve accessibility, responsive behavior, and performance despite strong visuals.

## Non-Goals

- Do not redesign the authenticated app shell in this phase.
- Do not change route structure, auth flow, or backend integrations.
- Do not use a default Vuetify marketing layout or a repeated equal-card grid as the main visual language.
- Do not use the old bright blue primary color as the landing page's dominant color.
- Do not use white page backgrounds on the landing page.

## Users And Context

The landing page is for viewers who need to feel that the project is professional and technically impressive: project evaluators, technical reviewers, and anyone seeing RetailOps before entering the app. They are not doing daily POS or warehouse work on this page; they are judging credibility, polish, and ambition.

The internal app users remain admins, sales staff, and warehouse staff. Their surfaces should stay readable and task-focused.

## Design Direction

Chosen direction: **Cinematic Command Center**.

Palette strategy: **Drenched dark** for the landing page. The page should be visually carried by dark atmosphere and committed accent light, not neutral white space.

Recommended palette:

- Background: obsidian, near-black charcoal, graphite layers.
- Primary energy: molten amber / orange-gold.
- Signal accents: controlled red for warnings and live status.
- Edge light: restrained violet for depth and premium contrast.
- Text: warm off-white, muted steel, and amber highlights. Avoid pure white blocks.

Scene sentence: a viewer opens the project at night and sees a high-end retail operations command center come online, with data pulses moving through POS, inventory, orders, and reports.

Reference anchors:

- 4K product trailer lighting and pacing.
- Sci-fi command center UI atmosphere, but cleaner and more product-focused.
- Premium enterprise launch page with cinematic motion, not cyberpunk clutter.

## Layout Strategy

The page should read as a trailer-like sequence rather than a conventional landing page.

1. **Hero: System Ignition**
   - Full first-viewport hero.
   - Brand/nav overlay is minimal and dark.
   - Headline is large, confident, and left-weighted or asymmetrically composed.
   - Floating dashboard/product preview sits in perspective with glow and parallax.
   - Primary CTA leads to `/login`; secondary CTA scrolls to the system story.
   - A hint of the next section must be visible on desktop and mobile.

2. **Operation Stack**
   - Show POS, inventory, goods receipt, debt, reports, and permissions as connected modules.
   - Avoid six identical cards. Use staggered panels, rails, or a command-map composition.

3. **Microservices Flow**
   - Visualize Vue frontend, API Gateway, Product & Inventory, Order & Sales, User & Report, SQL Server, and RabbitMQ.
   - Use moving packets or animated lines to imply event-driven data flow.
   - Keep labels short and legible.

4. **Role Workspaces**
   - Show Admin, Sales, and Warehouse as separate work modes.
   - Use distinct panels or split scenes, not decorative role cards.

5. **Live Metrics / Proof**
   - Use believable operational stats and status rows from the current project domain.
   - Avoid fake over-precise claims. Stats are visual credibility, not marketing guarantees.

6. **Final CTA**
   - Return to cinematic intensity with a compact, decisive login CTA.
   - Keep copy short and direct.

## Motion System

Motion is a first-class part of the design.

Required motion:

- Background data grid drifting slowly.
- Soft volumetric light sweep across the hero.
- Small particles or signal points moving along paths.
- Dashboard preview floating subtly in 3D space.
- Scroll reveal for major sections with staggered content, not identical fades everywhere.
- Microservice lines animating as data packets.
- Buttons and controls with tactile hover/active feedback.

Reduced motion:

- Respect `prefers-reduced-motion: reduce`.
- Disable continuous background movement, particle travel, and floating transforms.
- Keep static layered backgrounds, contrast, and depth so the page still looks designed.

Motion limits:

- Do not animate layout-heavy properties.
- Do not gate content visibility on JavaScript-only animation.
- Keep content visible by default and enhance with classes.

## Content Requirements

Primary headline should communicate command-center confidence, for example:

> RetailOps biến bán hàng, kho và báo cáo thành một trung tâm vận hành sống.

Supporting copy should explain the system in one short paragraph:

- POS bán hàng
- Quản lý đơn
- Tồn kho và nhập hàng
- Công nợ
- Báo cáo
- Phân quyền theo vai trò
- Microservices with API Gateway, SQL Server, RabbitMQ

Visible copy should be concise and in Vietnamese. Technical terms may remain English when they are product/architecture names: API Gateway, RabbitMQ, SQL Server, Microservices, POS.

## Component Plan

Implementation can stay in `landing.vue` if the page remains manageable. If the file becomes too large, split landing-only components under `src/components/landing/`.

Candidate components:

- `LandingCinematicBackground`
- `LandingHeroPreview`
- `LandingOperationMap`
- `LandingServiceFlow`
- `LandingRoleWorkspaces`
- `LandingMetricRail`

Do not introduce a new UI library. Continue using Vue 3, Vuetify 3, existing Remix/Iconify icons, and CSS/SVG/canvas-like native effects where appropriate.

## Accessibility

- Body text must meet WCAG AA contrast.
- Buttons must have clear focus states and readable labels.
- Motion must respect reduced-motion preference.
- Decorative visual layers should be `aria-hidden` when implemented as DOM elements.
- Navigation and CTAs must remain keyboard accessible.
- Text must not overflow on mobile.

## Responsive Behavior

Desktop:

- Use cinematic wide composition with layered hero preview.
- Keep nav on one line and under 80px high.
- Hero headline should fit without horizontal overflow.

Tablet:

- Stack hero copy and preview while preserving the command-center feel.
- Reduce motion amplitude and preview scale.

Mobile:

- Avoid tiny dashboards with unreadable text.
- Turn preview into a simplified compact command panel.
- Keep CTA visible in first viewport.
- Background motion should be quieter and lighter on GPU load.

## Technical Constraints

- Existing stack: Vue 3, TypeScript, Vite, Vuetify 3, Remix/Iconify icons.
- Do not assume packages that are not in `package.json`.
- No route or auth changes.
- No network-only visual assets required for the final page.
- Prefer CSS/SVG-native visuals over remote images so the page works offline.
- Avoid changing global Vuetify theme unless a landing-only override is insufficient.

## Verification Plan

Implementation should be verified with:

- `npm run typecheck`
- `npm run build`
- Browser visual check at `/`
- Desktop and mobile viewport inspection
- Reduced-motion inspection
- Contrast spot checks for hero text, CTA text, and muted body text

## Open Questions

No open design questions remain for the landing redesign. The approved direction is Cinematic Command Center with dark molten palette and strong background motion.
