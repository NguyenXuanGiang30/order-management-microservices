<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

const isRevealReady = ref(false)
let revealObserver: IntersectionObserver | null = null

onMounted(() => {
  isRevealReady.value = true

  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches)
    return

  revealObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting)
        return

      entry.target.classList.add('animate-in')
      revealObserver?.unobserve(entry.target)
    })
  }, {
    rootMargin: '0px 0px -90px 0px',
    threshold: 0.12,
  })

  document.querySelectorAll('.landing-reveal').forEach(element => {
    revealObserver?.observe(element)
  })
})

onBeforeUnmount(() => {
  revealObserver?.disconnect()
})

const modules = [
  {
    title: 'POS bán hàng',
    description: 'Tạo đơn nhanh tại quầy, chọn khách hàng, áp khuyến mãi và ghi nhận thanh toán trong cùng một luồng thao tác.',
    icon: 'ri-shopping-cart-2-line',
    tone: 'Bán hàng mượt mà',
  },
  {
    title: 'Kho hàng',
    description: 'Theo dõi sản phẩm, tồn kho, nhập xuất và kiểm kê bằng giao diện trực quan, dễ kiểm soát.',
    icon: 'ri-archive-line',
    tone: 'Tồn kho rõ ràng',
  },
  {
    title: 'Nhập hàng',
    description: 'Quản lý phiếu nhập, nhà cung cấp và cập nhật kho sau khi xác nhận nghiệp vụ.',
    icon: 'ri-truck-line',
    tone: 'Luồng nhập gọn',
  },
  {
    title: 'Công nợ',
    description: 'Ghi nhận thanh toán, theo dõi phải thu, phải trả và hỗ trợ đối soát sau bán hàng.',
    icon: 'ri-file-list-3-line',
    tone: 'Đối soát dễ dàng',
  },
  {
    title: 'Báo cáo',
    description: 'Tổng hợp dữ liệu vận hành thành các góc nhìn dễ hiểu cho người quản lý.',
    icon: 'ri-bar-chart-box-line',
    tone: 'Góc nhìn tổng quan',
  },
  {
    title: 'Phân quyền',
    description: 'Mỗi vai trò có workspace riêng, tập trung vào đúng nghiệp vụ cần xử lý.',
    icon: 'ri-shield-user-line',
    tone: 'Làm việc đúng vai trò',
  },
]

const roles = [
  {
    name: 'Admin',
    description: 'Không gian quản trị dành cho người điều hành, theo dõi hệ thống và kiểm soát dữ liệu.',
    icon: 'ri-shield-user-line',
  },
  {
    name: 'Sales',
    description: 'Không gian bán hàng dành cho nhân viên xử lý đơn, khách hàng và thanh toán.',
    icon: 'ri-shopping-bag-3-line',
  },
  {
    name: 'Warehouse',
    description: 'Không gian kho dành cho người phụ trách nhập hàng, tồn kho và kiểm kê.',
    icon: 'ri-archive-stack-line',
  },
]

const productFlow = [
  'Khách hàng mua hàng',
  'Nhân viên tạo đơn',
  'Kho được cập nhật',
  'Công nợ được ghi nhận',
  'Báo cáo sẵn sàng',
]
</script>

<template>
  <main
    class="retail-product-landing"
    :class="{ 'reveal-ready': isRevealReady }"
  >
    <div class="landing-mesh" />
    <div class="landing-grid" />
    <div class="landing-orb landing-orb--blue" />
    <div class="landing-orb landing-orb--cyan" />
    <div class="landing-orb landing-orb--violet" />

    <header class="landing-nav">
      <RouterLink
        to="/"
        class="landing-brand"
      >
        <VAvatar
          rounded="lg"
          size="42"
          class="landing-brand__icon"
        >
          <VIcon icon="ri-store-2-line" />
        </VAvatar>

        <span>Retail<span>Ops</span></span>
      </RouterLink>

      <nav class="landing-links">
        <a href="#overview">Tổng quan</a>
        <a href="#features">Chức năng</a>
        <a href="#roles">Vai trò</a>
      </nav>

      <VBtn
        to="/login"
        class="nav-login-btn"
        prepend-icon="ri-login-circle-line"
      >
        Đăng nhập
      </VBtn>
    </header>

    <section
      id="overview"
      class="landing-hero"
    >
      <div class="hero-copy">
        <div class="hero-badge">
          <VIcon icon="ri-sparkling-2-line" />
          <span>Nền tảng vận hành bán lẻ hiện đại</span>
        </div>

        <h1>
          Quản lý bán hàng và kho hàng trong một không gian liền mạch.
        </h1>

        <p>
          RetailOps giúp cửa hàng gom bán hàng, đơn hàng, tồn kho, nhập hàng, công nợ và báo cáo vào một giao diện trực quan, dễ dùng và phù hợp với từng vai trò vận hành.
        </p>

        <div class="hero-actions">
          <VBtn
            to="/login"
            size="x-large"
            class="primary-glow-btn"
            append-icon="ri-arrow-right-line"
          >
            Trải nghiệm hệ thống
          </VBtn>

          <VBtn
            href="#features"
            size="x-large"
            variant="outlined"
            class="ghost-btn"
          >
            Xem sản phẩm
          </VBtn>
        </div>
      </div>

      <aside class="product-showcase">
        <div class="showcase-orbit showcase-orbit--one" />
        <div class="showcase-orbit showcase-orbit--two" />

        <div class="showcase-window showcase-window--main">
          <div class="window-topbar">
            <div class="window-dots">
              <i />
              <i />
              <i />
            </div>
            <span>RetailOps workspace</span>
          </div>

          <div class="workspace-layout">
            <div class="workspace-sidebar">
              <span />
              <span />
              <span />
              <span />
            </div>

            <div class="workspace-content">
              <div class="workspace-heading">
                <div>
                  <small>Không gian vận hành</small>
                  <strong>Tổng quan sản phẩm</strong>
                </div>
                <VIcon icon="ri-dashboard-3-line" />
              </div>

              <div class="workspace-cards">
                <div>
                  <VIcon icon="ri-shopping-cart-2-line" />
                  <span>POS</span>
                </div>
                <div>
                  <VIcon icon="ri-archive-line" />
                  <span>Kho</span>
                </div>
                <div>
                  <VIcon icon="ri-file-list-3-line" />
                  <span>Công nợ</span>
                </div>
              </div>

              <div class="workspace-chart">
                <i />
                <i />
                <i />
                <i />
                <i />
              </div>
            </div>
          </div>
        </div>

        <div class="floating-card floating-card--flow">
          <strong>Luồng xử lý</strong>

          <div
            v-for="item in productFlow"
            :key="item"
            class="flow-item"
          >
            <VIcon icon="ri-checkbox-circle-line" />
            <span>{{ item }}</span>
          </div>
        </div>

        <div class="floating-card floating-card--role">
          <VIcon icon="ri-user-settings-line" />
          <strong>Workspace theo vai trò</strong>
          <p>Người dùng nhìn thấy đúng nghiệp vụ cần thao tác.</p>
        </div>

        <div class="floating-card floating-card--sync">
          <VIcon icon="ri-loop-left-line" />
          <strong>Dữ liệu kết nối</strong>
          <p>Bán hàng, kho và báo cáo được liên kết trong cùng một luồng.</p>
        </div>
      </aside>
    </section>

    <section class="intro-band landing-reveal">
      <div>
        <span class="section-kicker">Vấn đề RetailOps giải quyết</span>
        <h2>Từ thao tác rời rạc đến vận hành có kiểm soát.</h2>
      </div>

      <p>
        Thay vì quản lý bằng nhiều bảng tính và ghi chú riêng lẻ, RetailOps gom các nghiệp vụ quan trọng vào một hệ thống thống nhất, giúp đội ngũ bán hàng, kho và quản trị phối hợp rõ ràng hơn.
      </p>
    </section>

    <section
      id="features"
      class="landing-section landing-reveal"
    >
      <div class="section-heading">
        <span class="section-kicker">Chức năng sản phẩm</span>
        <h2>Mọi phân hệ được thiết kế để phục vụ thao tác hằng ngày.</h2>
        <p>
          Giao diện không chỉ để xem dữ liệu, mà để nhân viên thao tác nhanh, người quản lý dễ theo dõi và hệ thống vận hành nhất quán hơn.
        </p>
      </div>

      <div class="feature-grid">
        <article
          v-for="module in modules"
          :key="module.title"
          class="feature-card"
        >
          <div class="feature-icon">
            <VIcon :icon="module.icon" />
          </div>

          <span>{{ module.tone }}</span>
          <h3>{{ module.title }}</h3>
          <p>{{ module.description }}</p>
        </article>
      </div>
    </section>

    <section class="experience-section landing-section landing-reveal">
      <div class="section-heading">
        <span class="section-kicker">Trải nghiệm sử dụng</span>
        <h2>Một giao diện nhẹ, rõ và có cảm giác cao cấp.</h2>
        <p>
          RetailOps hướng tới trải nghiệm trực quan: ít nhiễu, dễ nhận biết luồng nghiệp vụ, chuyển động mượt và từng khu vực chức năng có vai trò rõ ràng.
        </p>
      </div>

      <div class="experience-grid">
        <article class="experience-card">
          <VIcon icon="ri-focus-3-line" />
          <h3>Tập trung vào thao tác</h3>
          <p>Người dùng nhìn thấy đúng hành động cần làm, không bị choáng bởi quá nhiều dữ liệu phụ.</p>
        </article>

        <article class="experience-card">
          <VIcon icon="ri-bubble-chart-line" />
          <h3>Liên kết nghiệp vụ</h3>
          <p>Đơn hàng, kho, công nợ và báo cáo được thể hiện như một luồng vận hành thống nhất.</p>
        </article>

        <article class="experience-card">
          <VIcon icon="ri-magic-line" />
          <h3>Giao diện giàu cảm xúc</h3>
          <p>Các lớp kính, ánh sáng và chuyển động nhẹ giúp sản phẩm hiện đại nhưng vẫn dễ sử dụng.</p>
        </article>
      </div>
    </section>

    <section
      id="roles"
      class="landing-section landing-reveal"
    >
      <div class="section-heading">
        <span class="section-kicker">Workspace theo nhiệm vụ</span>
        <h2>Mỗi nhóm người dùng có một không gian làm việc riêng.</h2>
        <p>
          RetailOps phân tách trải nghiệm theo vai trò để từng người dùng chỉ tập trung vào phần việc của mình.
        </p>
      </div>

      <div class="role-grid">
        <article
          v-for="role in roles"
          :key="role.name"
          class="role-card"
        >
          <VIcon :icon="role.icon" />
          <h3>{{ role.name }}</h3>
          <p>{{ role.description }}</p>
        </article>
      </div>
    </section>

    <section class="landing-cta landing-reveal">
      <div>
        <span class="section-kicker">Bắt đầu với RetailOps</span>
        <h2>Đưa toàn bộ vận hành bán lẻ về một nơi.</h2>
        <p>
          Đăng nhập để trải nghiệm không gian quản lý bán hàng, kho hàng, công nợ và báo cáo trong cùng một sản phẩm.
        </p>
      </div>

      <VBtn
        to="/login"
        size="x-large"
        class="primary-glow-btn"
        append-icon="ri-arrow-right-line"
      >
        Đi tới đăng nhập
      </VBtn>
    </section>
    <footer class="landing-footer">
      <div class="footer-main">
        <div class="footer-brand-area">
          <RouterLink
            to="/"
            class="footer-brand"
          >
            <VAvatar
              rounded="lg"
              size="44"
              class="footer-brand__icon"
            >
              <VIcon icon="ri-store-2-line" />
            </VAvatar>
            <span>Retail<span>Ops</span></span>
          </RouterLink>
          <p>
            Nền tảng quản lý bán lẻ giúp kết nối bán hàng, kho hàng, công nợ và báo cáo trong một không gian vận hành liền mạch.
          </p>
        </div>
        <div class="footer-columns">
          <div class="footer-column">
            <h4>Sản phẩm</h4>
            <a href="#overview">Tổng quan</a>
            <a href="#features">Chức năng</a>
            <a href="#roles">Vai trò</a>
          </div>
          <div class="footer-column">
            <h4>Phân hệ</h4>
            <span>POS bán hàng</span>
            <span>Kho hàng</span>
            <span>Công nợ</span>
            <span>Báo cáo</span>
          </div>
          <div class="footer-column">
            <h4>Truy cập</h4>
            <RouterLink to="/login">
              Đăng nhập hệ thống
            </RouterLink>
            <span>Workspace Admin</span>
            <span>Workspace Sales</span>
            <span>Workspace Warehouse</span>
          </div>
        </div>
      </div>
      <div class="footer-bottom">
        <span>© RetailOps. All rights reserved.</span>
        <div class="footer-status">
          <i />
          <span>Product interface concept</span>
        </div>
      </div>
    </footer>
  </main>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700;800;900&display=swap');

.retail-product-landing {
  font-family: 'Be Vietnam Pro', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}
.retail-product-landing {
  position: relative;
  isolation: isolate;
  min-block-size: 100dvh;
  overflow: hidden;
  background:
    radial-gradient(circle at 70% 8%, rgba(56, 189, 248, 0.22), transparent 32rem),
    radial-gradient(circle at 8% 22%, rgba(37, 99, 235, 0.22), transparent 34rem),
    radial-gradient(circle at 50% 88%, rgba(168, 85, 247, 0.14), transparent 34rem),
    #020617;
  color: #f8fafc;
}

.landing-mesh {
  position: fixed;
  inset: 0;
  z-index: -6;
  background:
    linear-gradient(115deg, rgba(15, 23, 42, 0.88), rgba(2, 6, 23, 0.92)),
    radial-gradient(circle at 30% 20%, rgba(14, 165, 233, 0.14), transparent 28rem);
}

.landing-grid {
  position: fixed;
  inset: 0;
  z-index: -5;
  opacity: 0.22;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(148, 163, 184, 0.08) 1px, transparent 1px),
    linear-gradient(90deg, rgba(148, 163, 184, 0.08) 1px, transparent 1px);
  background-size: 52px 52px;
  mask-image: radial-gradient(circle at center, black 0%, transparent 78%);
}

.landing-orb {
  position: fixed;
  z-index: -4;
  border-radius: 999px;
  filter: blur(44px);
  pointer-events: none;
}

.landing-orb--blue {
  inline-size: 440px;
  block-size: 440px;
  inset-block-start: 12%;
  inset-inline-start: -8%;
  background: rgba(37, 99, 235, 0.24);
  animation: orbFloat 18s ease-in-out infinite;
}

.landing-orb--cyan {
  inline-size: 520px;
  block-size: 520px;
  inset-block-start: 6%;
  inset-inline-end: -10%;
  background: rgba(34, 211, 238, 0.18);
  animation: orbFloat 22s ease-in-out infinite reverse;
}

.landing-orb--violet {
  inline-size: 360px;
  block-size: 360px;
  inset-block-end: 8%;
  inset-inline-start: 34%;
  background: rgba(168, 85, 247, 0.14);
  animation: orbFloat 20s ease-in-out infinite;
}

/* Nav */
.landing-nav {
  position: sticky;
  inset-block-start: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-inline-size: 1240px;
  margin-inline: auto;
  padding: 1rem clamp(1rem, 4vw, 2rem);
  animation: fadeDown 0.65s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.landing-nav::before {
  content: '';
  position: absolute;
  inset: 0.5rem clamp(0.5rem, 3vw, 1rem);
  z-index: -1;
  border: 1px solid rgba(125, 211, 252, 0.18);
  border-radius: 999px;
  background: rgba(2, 6, 23, 0.44);
  box-shadow:
    0 18px 50px rgba(0, 0, 0, 0.25),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(22px);
}

.landing-brand {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  color: white;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-decoration: none;
  text-transform: uppercase;
}

.landing-brand__icon {
  color: white;
  background:
    radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.42), transparent 28%),
    linear-gradient(135deg, #2563eb, #0ea5e9);
  box-shadow: 0 14px 34px rgba(37, 99, 235, 0.42);
}

.landing-brand span span {
  color: #3b82f6;
}

.landing-links {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.landing-links a {
  color: rgba(226, 232, 240, 0.76);
  font-size: 0.92rem;
  font-weight: 700;
  text-decoration: none;
}

.landing-links a:hover {
  color: #67e8f9;
}

.nav-login-btn {
  border: 1px solid rgba(96, 165, 250, 0.34);
  color: white !important;
  background: rgba(37, 99, 235, 0.18) !important;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

/* Hero */
.landing-hero {
  display: grid;
  align-items: center;
  max-inline-size: 1240px;
  min-block-size: calc(100dvh - 86px);
  gap: clamp(2rem, 6vw, 5rem);
  grid-template-columns: minmax(0, 0.94fr) minmax(420px, 1fr);
  margin-inline: auto;
  padding: clamp(3rem, 7vw, 6rem) clamp(1rem, 4vw, 2rem);
}

.hero-copy {
  position: relative;
  z-index: 3;
}

.hero-badge,
.section-kicker {
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  font-weight: 600;
  letter-spacing: -0.005em;
  text-transform: none;
}

.hero-badge {
  padding: 0.55rem 0.9rem;
  margin-block-end: 1.35rem;
  font-size: 0.86rem;
  font-weight: 800;
}

.section-kicker {
  padding: 0.45rem 0.85rem;
  margin-block-end: 1rem;
  font-size: 0.78rem;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.hero-copy h1 {
  max-inline-size: 780px;
  margin: 0 0 1.4rem;
  color: white;
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  font-size: clamp(2.8rem, 5.8vw, 5.6rem);
  font-weight: 800;
  letter-spacing: -0.045em;
  line-height: 1.02;
  text-shadow: 0 0 44px rgba(255, 255, 255, 0.12);
}

.hero-copy p {
  max-inline-size: 660px;
  margin: 0 0 2rem;
  color: rgba(226, 232, 240, 0.72);
  font-size: clamp(1rem, 1.8vw, 1.18rem);
  line-height: 1.75;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.9rem;
}

.primary-glow-btn {
  min-block-size: 54px;
  border-radius: 16px;
  color: white !important;
  font-weight: 900;
  background:
    linear-gradient(90deg, #0f62fe 0%, #2563eb 46%, #00a6ff 100%) !important;
  box-shadow:
    0 20px 50px rgba(37, 99, 235, 0.42),
    inset 0 1px 0 rgba(255, 255, 255, 0.35);
  overflow: hidden;
}

.primary-glow-btn::after {
  content: '';
  position: absolute;
  inset-block: 0;
  inset-inline-start: -40%;
  inline-size: 28%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.44), transparent);
  transform: skewX(-18deg);
  animation: buttonShine 3.4s ease-in-out infinite;
}

.ghost-btn {
  min-block-size: 54px;
  border-radius: 16px;
  color: #dbeafe !important;
  border-color: rgba(147, 197, 253, 0.3) !important;
  background: rgba(15, 23, 42, 0.28);
  backdrop-filter: blur(16px);
}

/* Product showcase */
.product-showcase {
  position: relative;
  min-block-size: 620px;
  perspective: 1400px;
  animation: systemEnter 0.9s cubic-bezier(0.22, 1, 0.36, 1) 0.2s both;
}

.showcase-window,
.floating-card {
  position: absolute;
  border: 1px solid rgba(103, 232, 249, 0.24);
  border-radius: 28px;
  background:
    linear-gradient(145deg, rgba(15, 23, 42, 0.76), rgba(8, 47, 73, 0.42)),
    rgba(2, 6, 23, 0.38);
  box-shadow:
    0 24px 80px rgba(0, 0, 0, 0.38),
    inset 0 1px 0 rgba(255, 255, 255, 0.14),
    0 0 46px rgba(34, 211, 238, 0.12);
  backdrop-filter: blur(24px);
  transform-style: preserve-3d;
}

.showcase-window--main {
  inset-block-start: 7%;
  inset-inline-start: 0;
  inline-size: min(100%, 590px);
  padding: 1rem;
  transform: rotateY(-11deg) rotateX(5deg);
  animation: cardFloat 8.5s ease-in-out infinite;
}

.window-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.4rem 0.35rem 1rem;
  color: rgba(226, 232, 240, 0.68);
  font-size: 0.86rem;
  font-weight: 800;
}

.window-dots {
  display: flex;
  gap: 0.45rem;
}

.window-dots i {
  display: block;
  inline-size: 10px;
  block-size: 10px;
  border-radius: 50%;
  background: rgba(148, 163, 184, 0.5);
}

.workspace-layout {
  display: grid;
  gap: 1rem;
  grid-template-columns: 74px 1fr;
  border-radius: 22px;
  padding: 1rem;
  background:
    radial-gradient(circle at 75% 16%, rgba(34, 211, 238, 0.16), transparent 30%),
    rgba(2, 6, 23, 0.34);
}

.workspace-sidebar {
  display: grid;
  gap: 0.8rem;
  align-content: start;
  border-radius: 18px;
  padding: 0.8rem;
  background: rgba(15, 23, 42, 0.55);
}

.workspace-sidebar span {
  block-size: 34px;
  border-radius: 12px;
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.8), rgba(34, 211, 238, 0.5));
}

.workspace-content {
  min-block-size: 330px;
}

.workspace-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-block-end: 1rem;
}

.workspace-heading small {
  display: block;
  color: rgba(226, 232, 240, 0.58);
}

.workspace-heading strong {
  display: block;
  color: white;
  font-size: 1.35rem;
}

.workspace-heading .v-icon {
  color: #67e8f9;
  font-size: 2rem;
}

.workspace-cards {
  display: grid;
  gap: 0.8rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.workspace-cards div {
  display: grid;
  gap: 0.5rem;
  place-items: center;
  min-block-size: 98px;
  border: 1px solid rgba(125, 211, 252, 0.16);
  border-radius: 18px;
  color: rgba(226, 232, 240, 0.84);
  font-weight: 800;
  background: rgba(15, 23, 42, 0.42);
}

.workspace-cards .v-icon {
  color: #60a5fa;
  font-size: 1.6rem;
}

.workspace-chart {
  display: flex;
  align-items: end;
  gap: 1rem;
  block-size: 150px;
  padding: 1rem;
  border-radius: 18px;
  background:
    linear-gradient(rgba(148, 163, 184, 0.08) 1px, transparent 1px),
    rgba(15, 23, 42, 0.3);
  background-size: 100% 34px;
}

.workspace-chart i {
  flex: 1;
  border-radius: 999px 999px 0 0;
  background: linear-gradient(180deg, #22d3ee, rgba(34, 211, 238, 0.08));
  box-shadow: 0 0 22px rgba(34, 211, 238, 0.3);
}

.workspace-chart i:nth-child(1) { block-size: 36%; }
.workspace-chart i:nth-child(2) { block-size: 62%; }
.workspace-chart i:nth-child(3) { block-size: 48%; }
.workspace-chart i:nth-child(4) { block-size: 78%; }
.workspace-chart i:nth-child(5) { block-size: 58%; }

.floating-card {
  padding: 1.15rem;
  animation: cardFloat 8s ease-in-out infinite;
}

.floating-card strong {
  display: block;
  color: white;
  margin-block-end: 0.75rem;
}

.floating-card p {
  margin: 0;
  color: rgba(226, 232, 240, 0.68);
  line-height: 1.6;
}

.floating-card .v-icon {
  color: #67e8f9;
  font-size: 1.6rem;
  margin-block-end: 0.65rem;
}

.floating-card--flow {
  inset-block-end: 8%;
  inset-inline-start: 5%;
  inline-size: 310px;
  transform: rotateY(-10deg) rotateX(6deg);
  animation-delay: -1.4s;
}

.flow-item {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  margin-block-start: 0.78rem;
  color: rgba(226, 232, 240, 0.78);
  font-size: 0.86rem;
}

.flow-item .v-icon {
  color: #4ade80;
  font-size: 1rem;
  margin: 0;
}

.floating-card--role {
  inset-block-start: 16%;
  inset-inline-end: 0;
  inline-size: 260px;
  transform: rotateY(-16deg) rotateX(6deg);
  animation-delay: -0.8s;
}

.floating-card--sync {
  inset-block-end: 4%;
  inset-inline-end: 12%;
  inline-size: 260px;
  transform: rotateY(-12deg) rotateX(6deg);
  animation-delay: -2.2s;
}

.showcase-orbit {
  position: absolute;
  border: 1px solid rgba(125, 211, 252, 0.16);
  border-radius: 50%;
  filter: drop-shadow(0 0 18px rgba(34, 211, 238, 0.18));
  pointer-events: none;
}

.showcase-orbit--one {
  inline-size: 640px;
  block-size: 240px;
  inset-block-start: 34%;
  inset-inline-start: -6%;
  transform: rotate(-12deg);
}

.showcase-orbit--two {
  inline-size: 520px;
  block-size: 190px;
  inset-block-start: 50%;
  inset-inline-start: 18%;
  transform: rotate(18deg);
}

/* Sections */
.intro-band,
.landing-section,
.landing-cta {
  position: relative;
  z-index: 2;
  max-inline-size: 1240px;
  margin-inline: auto;
  padding-inline: clamp(1rem, 4vw, 2rem);
}

.intro-band {
  display: grid;
  align-items: center;
  gap: 2rem;
  grid-template-columns: minmax(0, 0.95fr) minmax(320px, 0.86fr);
  border: 1px solid rgba(125, 211, 252, 0.18);
  border-radius: 30px;
  margin-block: 1rem 5rem;
  padding-block: 2rem;
  background: rgba(15, 23, 42, 0.36);
  box-shadow:
    0 24px 90px rgba(0, 0, 0, 0.28),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(22px);
}

.intro-band h2,
.section-heading h2,
.landing-cta h2 {
  margin: 0;
  color: white;
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  font-size: clamp(2rem, 4.2vw, 3.6rem);
  font-weight: 800;
  letter-spacing: -0.04em;
  line-height: 1.08;
}

.hero-copy p,
.intro-band p,
.section-heading p,
.landing-cta p {
  color: rgba(226, 232, 240, 0.72);
  font-size: clamp(1rem, 1.5vw, 1.12rem);
  font-weight: 400;
  line-height: 1.85;
  letter-spacing: -0.01em;
}

.section-heading p,
.landing-cta p {
  margin-block-start: 1rem;
}

.landing-section {
  padding-block: clamp(4rem, 8vw, 7rem);
}

.section-heading {
  max-inline-size: 820px;
  margin-block-end: 2.3rem;
}

/* Cards */
.feature-grid,
.experience-grid,
.role-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.feature-card,
.experience-card,
.role-card {
  position: relative;
  overflow: hidden;
  border: 1px solid rgba(125, 211, 252, 0.18);
  border-radius: 24px;
  padding: 1.35rem;
  background:
    linear-gradient(145deg, rgba(15, 23, 42, 0.72), rgba(8, 47, 73, 0.34)),
    rgba(2, 6, 23, 0.36);
  box-shadow:
    0 20px 58px rgba(0, 0, 0, 0.26),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(20px);
  transition:
    transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
    border-color 260ms ease,
    box-shadow 260ms ease;
}

.feature-card::before,
.experience-card::before,
.role-card::before {
  content: '';
  position: absolute;
  inset-inline: 1.4rem;
  inset-block-start: 0;
  block-size: 3px;
  border-radius: 999px;
  background: linear-gradient(90deg, transparent, #60a5fa, #22d3ee, transparent);
  opacity: 0.85;
}

.feature-icon {
  display: grid;
  place-items: center;
  inline-size: 52px;
  block-size: 52px;
  border-radius: 16px;
  margin-block-end: 1rem;
  color: white;
  background: linear-gradient(135deg, #2563eb, #22d3ee);
  box-shadow: 0 16px 38px rgba(37, 99, 235, 0.34);
}

.feature-card > span {
  display: inline-flex;
  margin-block-end: 0.7rem;
  color: #86efac;
  font-size: 0.8rem;
  font-weight: 900;
}

.feature-card h3,
.experience-card h3,
.role-card h3 {
  color: white;
  font-size: 1.15rem;
  margin: 0 0 0.65rem;
}

.landing-brand,
.feature-card h3,
.experience-card h3,
.role-card h3,
.footer-brand {
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  font-weight: 700;
  letter-spacing: -0.015em;
}

.landing-links a,
.nav-login-btn,
.primary-glow-btn,
.ghost-btn {
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  font-weight: 600;
}

.experience-card .v-icon,
.role-card .v-icon {
  color: #60a5fa;
  font-size: 2.1rem;
  margin-block-end: 1rem;
}

.experience-section {
  border-block: 1px solid rgba(125, 211, 252, 0.12);
}

/* CTA */
.landing-cta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  border: 1px solid rgba(125, 211, 252, 0.2);
  border-radius: 34px;
  margin-block: 3rem 5rem;
  padding-block: 2.4rem;
  background:
    radial-gradient(circle at 82% 20%, rgba(34, 211, 238, 0.18), transparent 32%),
    linear-gradient(145deg, rgba(15, 23, 42, 0.72), rgba(8, 47, 73, 0.34));
  box-shadow:
    0 24px 90px rgba(0, 0, 0, 0.32),
    inset 0 1px 0 rgba(255, 255, 255, 0.09);
  backdrop-filter: blur(22px);
}

.landing-cta > div {
  max-inline-size: 760px;
}

/* Motion */
.reveal-ready .landing-reveal {
  opacity: 0;
  transform: translateY(34px);
  transition:
    opacity 0.72s cubic-bezier(0.22, 1, 0.36, 1),
    transform 0.72s cubic-bezier(0.22, 1, 0.36, 1);
}

.reveal-ready .landing-reveal.animate-in {
  opacity: 1;
  transform: translateY(0);
}

.hero-copy > * {
  animation: fadeUp 0.7s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.hero-copy > *:nth-child(1) { animation-delay: 0.06s; }
.hero-copy > *:nth-child(2) { animation-delay: 0.14s; }
.hero-copy > *:nth-child(3) { animation-delay: 0.22s; }
.hero-copy > *:nth-child(4) { animation-delay: 0.3s; }

.feature-card:hover,
.experience-card:hover,
.role-card:hover {
  border-color: rgba(103, 232, 249, 0.38);
  box-shadow:
    0 28px 80px rgba(0, 0, 0, 0.36),
    0 0 54px rgba(34, 211, 238, 0.12),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  transform: translateY(-8px);
}

@keyframes fadeDown {
  from {
    opacity: 0;
    transform: translateY(-18px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeUp {
  from {
    opacity: 0;
    transform: translateY(26px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes systemEnter {
  from {
    opacity: 0;
    transform: translateX(36px) scale(0.96);
  }

  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes cardFloat {
  0%, 100% {
    translate: 0 0;
  }

  50% {
    translate: 0 -16px;
  }
}

@keyframes orbFloat {
  0%, 100% {
    transform: translate3d(0, 0, 0) scale(1);
  }

  50% {
    transform: translate3d(30px, -24px, 0) scale(1.08);
  }
}

@keyframes buttonShine {
  0%, 45% {
    transform: translateX(0) skewX(-18deg);
  }

  70%, 100% {
    transform: translateX(540%) skewX(-18deg);
  }
}

/* Responsive */
@media (max-width: 1180px) {
  .landing-hero,
  .intro-band {
    grid-template-columns: 1fr;
  }

  .product-showcase {
    min-block-size: 600px;
  }

  .feature-grid,
  .experience-grid,
  .role-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .showcase-window--main {
    inset-inline-start: 0;
  }
}

@media (max-width: 760px) {
  .landing-nav {
    gap: 1rem;
  }

  .landing-nav::before {
    border-radius: 24px;
  }

  .landing-links {
    display: none;
  }

  .landing-brand {
    font-size: 0.95rem;
  }

  .nav-login-btn {
    padding-inline: 0.8rem;
  }

  .landing-hero {
    min-block-size: auto;
    padding-block-start: 4rem;
  }

  .hero-copy h1 {
    font-size: clamp(2.55rem, 15vw, 4.3rem);
  }

  .feature-grid,
  .experience-grid,
  .role-grid {
    grid-template-columns: 1fr;
  }

  .product-showcase {
    min-block-size: 520px;
    transform: scale(0.82);
    transform-origin: top center;
    margin-block-end: -5rem;
  }

  .showcase-window--main {
    inline-size: 560px;
  }

  .floating-card--role {
    inset-inline-end: auto;
    inset-inline-start: 52%;
  }

  .floating-card--sync {
    inset-inline-end: 16%;
  }

  .landing-cta {
    align-items: flex-start;
    flex-direction: column;
  }
}

@media (max-width: 520px) {
  .product-showcase {
    display: none;
  }

  .landing-hero {
    padding-block-end: 3rem;
  }

  .hero-actions .v-btn {
    inline-size: 100%;
  }

  .intro-band,
  .landing-cta {
    border-radius: 24px;
  }
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }

  .reveal-ready .landing-reveal {
    opacity: 1;
    transform: none;
  }
}
/* Footer */
.landing-footer {
  position: relative;
  z-index: 2;
  max-inline-size: 1240px;
  margin-inline: auto;
  padding: 0 clamp(1rem, 4vw, 2rem) 2rem;
}

.footer-main {
  display: grid;
  gap: 3rem;
  grid-template-columns: minmax(0, 1fr) minmax(520px, 0.95fr);
  border: 1px solid rgba(125, 211, 252, 0.16);
  border-radius: 34px;
  padding: clamp(1.5rem, 4vw, 2.5rem);
  background:
    radial-gradient(circle at 12% 0%, rgba(37, 99, 235, 0.18), transparent 34%),
    linear-gradient(145deg, rgba(15, 23, 42, 0.7), rgba(8, 47, 73, 0.28));
  box-shadow:
    0 24px 90px rgba(0, 0, 0, 0.28),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(22px);
}

.footer-brand {
  display: inline-flex;
  align-items: center;
  gap: 0.8rem;
  color: white;
  font-weight: 950;
  letter-spacing: 0.04em;
  text-decoration: none;
  text-transform: uppercase;
}

.footer-brand__icon {
  color: white;
  background:
    radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.42), transparent 28%),
    linear-gradient(135deg, #2563eb, #0ea5e9);
  box-shadow: 0 14px 34px rgba(37, 99, 235, 0.42);
}

.footer-brand span span {
  color: #3b82f6;
}

.footer-brand-area p {
  max-inline-size: 520px;
  margin: 1.2rem 0 0;
  color: rgba(226, 232, 240, 0.64);
  line-height: 1.75;
}

.footer-columns {
  display: grid;
  gap: 1.5rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.footer-column {
  display: flex;
  flex-direction: column;
  gap: 0.72rem;
}

.footer-column h4 {
  margin: 0 0 0.3rem;
  color: white;
  font-size: 0.95rem;
  font-weight: 900;
}

.footer-column a,
.footer-column span {
  color: rgba(226, 232, 240, 0.62);
  font-size: 0.92rem;
  text-decoration: none;
  transition: color 180ms ease, transform 180ms ease;
}

.footer-column a:hover {
  color: #67e8f9;
  transform: translateX(4px);
}

.footer-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.2rem 0 0;
  color: rgba(226, 232, 240, 0.5);
  font-size: 0.88rem;
}

.footer-status {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.footer-status i {
  inline-size: 8px;
  block-size: 8px;
  border-radius: 50%;
  background: #22c55e;
  box-shadow: 0 0 18px rgba(34, 197, 94, 0.8);
}

/* Footer responsive */
@media (max-width: 900px) {
  .footer-main {
    grid-template-columns: 1fr;
  }

  .footer-columns {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 560px) {
  .footer-main {
    border-radius: 24px;
  }

  .footer-columns {
    grid-template-columns: 1fr;
  }

  .footer-bottom {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>