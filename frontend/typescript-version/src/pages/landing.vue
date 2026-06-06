<script setup lang="ts">
const modules = [
  {
    title: 'POS bán hàng',
    description: 'Tạo đơn nhanh tại quầy, chọn khách hàng, áp khuyến mãi và ghi nhận thanh toán.',
    icon: 'ri-shopping-cart-2-line',
  },
  {
    title: 'Kho hàng',
    description: 'Theo dõi tồn khả dụng, cảnh báo thấp kho, kiểm kê và lịch sử biến động.',
    icon: 'ri-archive-line',
  },
  {
    title: 'Nhập hàng',
    description: 'Lập phiếu nhập từ nhà cung cấp, xác nhận phiếu và cập nhật tồn kho.',
    icon: 'ri-truck-line',
  },
  {
    title: 'Công nợ',
    description: 'Theo dõi phải thu khách hàng, phải trả nhà cung cấp và ghi nhận thanh toán.',
    icon: 'ri-file-list-3-line',
  },
  {
    title: 'Báo cáo',
    description: 'Tổng hợp doanh thu, lợi nhuận, top sản phẩm và top khách hàng.',
    icon: 'ri-bar-chart-box-line',
  },
  {
    title: 'Phân quyền',
    description: 'Ba workspace rõ ràng cho Admin, Sales và Warehouse.',
    icon: 'ri-shield-user-line',
  },
]

const services = [
  {
    name: 'Product & Inventory',
    text: 'Sản phẩm, danh mục, tồn kho, nhập hàng và kiểm kê.',
  },
  {
    name: 'Order & Sales',
    text: 'Đơn hàng, khách hàng, nhà cung cấp, thanh toán và công nợ.',
  },
  {
    name: 'User & Report',
    text: 'JWT auth, người dùng, báo cáo, thông báo, audit và backup.',
  },
]

const roles = [
  {
    name: 'Admin',
    description: 'Xem dashboard, quản trị dữ liệu, phân quyền và báo cáo.',
  },
  {
    name: 'Sales',
    description: 'Bán hàng POS, quản lý đơn, khách hàng và công nợ phải thu.',
  },
  {
    name: 'Warehouse',
    description: 'Theo dõi tồn kho, nhập hàng, kiểm kê và nhà cung cấp.',
  },
]
</script>

<template>
  <main class="retail-landing">
    <header class="landing-nav">
      <RouterLink
        to="/"
        class="landing-brand"
      >
        <VAvatar
          color="primary"
          rounded="lg"
          size="38"
        >
          <VIcon icon="ri-store-2-line" />
        </VAvatar>
        <span>RetailOps</span>
      </RouterLink>

      <nav class="landing-links">
        <a href="#features">Chức năng</a>
        <a href="#architecture">Kiến trúc</a>
        <a href="#roles">Vai trò</a>
      </nav>

      <VBtn
        to="/login"
        color="primary"
        prepend-icon="ri-login-circle-line"
      >
        Đăng nhập
      </VBtn>
    </header>

    <section class="landing-hero">
      <div class="hero-copy">
        <VChip
          color="primary"
          variant="tonal"
          class="mb-5"
        >
          Vue 3 + Vuetify + ASP.NET Core Microservices
        </VChip>

        <h1>Quản lý bán hàng và kho hàng trong một hệ thống vận hành gọn.</h1>
        <p>
          RetailOps gom POS, đơn hàng, tồn kho, nhập hàng, công nợ và báo cáo vào một giao diện dành cho cửa hàng nhỏ và bài tập lớn microservices.
        </p>

        <div class="hero-actions">
          <VBtn
            to="/login"
            color="primary"
            size="large"
            append-icon="ri-arrow-right-line"
          >
            Đăng nhập hệ thống
          </VBtn>
          <VBtn
            href="#features"
            variant="outlined"
            size="large"
          >
            Xem chức năng
          </VBtn>
        </div>
      </div>

      <aside class="hero-preview">
        <div class="preview-toolbar">
          <span>RetailOps dashboard</span>
          <VChip
            color="success"
            size="small"
            variant="tonal"
          >
            Online
          </VChip>
        </div>

        <div class="preview-grid">
          <VCard class="preview-metric">
            <VCardText>
              <div class="text-caption text-medium-emphasis">
                Doanh thu hôm nay
              </div>
              <strong>12.450.800 đ</strong>
            </VCardText>
          </VCard>
          <VCard class="preview-metric">
            <VCardText>
              <div class="text-caption text-medium-emphasis">
                SKU cần nhập
              </div>
              <strong>2</strong>
            </VCardText>
          </VCard>
        </div>

        <VCard class="preview-table">
          <VCardItem>
            <VCardTitle>Luồng vận hành</VCardTitle>
            <VCardSubtitle>POS, kho và công nợ cập nhật qua API Gateway</VCardSubtitle>
          </VCardItem>
          <VList density="compact">
            <VListItem
              title="Tạo đơn POS"
              subtitle="Kiểm tra tồn kho trước khi bán"
            >
              <template #prepend>
                <VIcon icon="ri-checkbox-circle-line" color="success" />
              </template>
            </VListItem>
            <VListItem
              title="Xác nhận phiếu nhập"
              subtitle="Cộng tồn và cập nhật giá nhập"
            >
              <template #prepend>
                <VIcon icon="ri-checkbox-circle-line" color="success" />
              </template>
            </VListItem>
            <VListItem
              title="Theo dõi công nợ"
              subtitle="Khách hàng và nhà cung cấp"
            >
              <template #prepend>
                <VIcon icon="ri-time-line" color="warning" />
              </template>
            </VListItem>
          </VList>
        </VCard>
      </aside>
    </section>

    <section class="landing-band problem-band">
      <div>
        <h2>Từ Excel rời rạc sang quy trình có kiểm soát.</h2>
        <p>
          Hệ thống giảm sai lệch tồn kho, chuẩn hóa luồng bán hàng, ghi nhận công nợ và giúp từng vai trò chỉ thấy đúng nghiệp vụ cần xử lý.
        </p>
      </div>
      <div class="problem-list">
        <span>Lệch tồn kho</span>
        <span>Khó đối soát đơn</span>
        <span>Không rõ công nợ</span>
        <span>Báo cáo thủ công</span>
      </div>
    </section>

    <section
      id="features"
      class="landing-section"
    >
      <div class="section-heading">
        <h2>Các phân hệ chính</h2>
        <p>Những màn nghiệp vụ trong app nội bộ được thiết kế cho thao tác hằng ngày, không phải trang trình diễn.</p>
      </div>

      <div class="feature-grid">
        <VCard
          v-for="module in modules"
          :key="module.title"
          class="feature-card"
        >
          <VCardText>
            <VAvatar
              color="primary"
              variant="tonal"
              rounded="lg"
              class="mb-4"
            >
              <VIcon :icon="module.icon" />
            </VAvatar>
            <h3>{{ module.title }}</h3>
            <p>{{ module.description }}</p>
          </VCardText>
        </VCard>
      </div>
    </section>

    <section
      id="architecture"
      class="landing-section architecture-section"
    >
      <div class="section-heading">
        <h2>Kiến trúc microservices đúng yêu cầu đề tài</h2>
        <p>Frontend gọi API qua Gateway, mỗi service sở hữu database riêng và đồng bộ nghiệp vụ bằng REST/event.</p>
      </div>

      <div class="architecture-flow">
        <VCard class="gateway-card">
          <VCardText>
            <VIcon icon="ri-route-line" size="28" />
            <h3>API Gateway</h3>
            <p>Điểm vào duy nhất, định tuyến request và chuyển tiếp JWT tới service phù hợp.</p>
          </VCardText>
        </VCard>

        <VCard
          v-for="service in services"
          :key="service.name"
          class="service-card"
        >
          <VCardText>
            <h3>{{ service.name }}</h3>
            <p>{{ service.text }}</p>
          </VCardText>
        </VCard>
      </div>
    </section>

    <section
      id="roles"
      class="landing-section"
    >
      <div class="section-heading">
        <h2>Ba vai trò, ba luồng thao tác rõ ràng</h2>
        <p>RetailOps tách quyền theo đúng mô hình Admin, Sales và Warehouse.</p>
      </div>

      <div class="role-grid">
        <VCard
          v-for="role in roles"
          :key="role.name"
          class="role-card"
        >
          <VCardText>
            <span>{{ role.name }}</span>
            <p>{{ role.description }}</p>
          </VCardText>
        </VCard>
      </div>
    </section>

    <section class="landing-cta">
      <div>
        <h2>Sẵn sàng vào workspace RetailOps?</h2>
        <p>Đăng nhập để quản lý bán hàng, kho, công nợ và báo cáo trong app nội bộ.</p>
      </div>
      <VBtn
        to="/login"
        color="primary"
        size="large"
        append-icon="ri-arrow-right-line"
      >
        Đi tới đăng nhập
      </VBtn>
    </section>
  </main>
</template>

<style scoped>
.retail-landing {
  min-block-size: 100dvh;
  background:
    linear-gradient(180deg, rgba(var(--v-theme-primary), 0.06), transparent 34rem),
    rgb(var(--v-theme-background));
  color: rgb(var(--v-theme-on-background));
}

.landing-nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  max-inline-size: 1180px;
  margin-inline: auto;
  padding: 1.25rem clamp(1rem, 4vw, 2rem);
}

.landing-brand {
  display: inline-flex;
  align-items: center;
  color: inherit;
  column-gap: 0.75rem;
  font-size: 1.05rem;
  font-weight: 800;
  text-decoration: none;
}

.landing-links {
  display: flex;
  gap: 1.5rem;
}

.landing-links a {
  color: rgba(var(--v-theme-on-background), 0.72);
  font-weight: 600;
  text-decoration: none;
}

.landing-links a:hover {
  color: rgb(var(--v-theme-primary));
}

.landing-hero {
  display: grid;
  align-items: center;
  max-inline-size: 1180px;
  gap: clamp(2rem, 6vw, 5rem);
  grid-template-columns: minmax(0, 1fr) minmax(360px, 0.78fr);
  margin-inline: auto;
  padding: clamp(3rem, 8vw, 6rem) clamp(1rem, 4vw, 2rem);
}

.hero-copy h1 {
  max-inline-size: 780px;
  font-size: clamp(2.7rem, 6vw, 5.4rem);
  font-weight: 850;
  letter-spacing: 0;
  line-height: 0.98;
  margin-block: 0 1.5rem;
}

.hero-copy p {
  max-inline-size: 660px;
  color: rgba(var(--v-theme-on-background), 0.72);
  font-size: clamp(1rem, 2vw, 1.18rem);
  line-height: 1.7;
  margin-block-end: 2rem;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.875rem;
}

.hero-preview {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 18px;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 22px 70px rgba(20, 20, 43, 0.12);
  padding: 1rem;
}

.preview-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-block-end: 1rem;
}

.preview-toolbar span {
  font-weight: 800;
}

.preview-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.preview-metric strong {
  display: block;
  font-size: 1.35rem;
  margin-block-start: 0.35rem;
}

.landing-band,
.landing-section,
.landing-cta {
  max-inline-size: 1180px;
  margin-inline: auto;
  padding-inline: clamp(1rem, 4vw, 2rem);
}

.problem-band {
  display: grid;
  align-items: center;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 18px;
  background: rgb(var(--v-theme-surface));
  gap: 2rem;
  grid-template-columns: minmax(0, 1fr) minmax(280px, 0.8fr);
  margin-block: 1rem 4rem;
  padding-block: 2rem;
}

.problem-band h2,
.landing-section h2,
.landing-cta h2 {
  font-size: clamp(2rem, 4vw, 3.2rem);
  font-weight: 820;
  letter-spacing: 0;
  line-height: 1.08;
  margin-block: 0 1rem;
}

.problem-band p,
.section-heading p,
.landing-cta p,
.architecture-flow p,
.feature-card p,
.role-card p {
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.65;
}

.problem-list {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.problem-list span {
  border-radius: 999px;
  background: rgba(var(--v-theme-primary), 0.08);
  color: rgb(var(--v-theme-primary));
  font-weight: 700;
  padding: 0.7rem 1rem;
  text-align: center;
}

.landing-section {
  padding-block: 4rem;
}

.section-heading {
  max-inline-size: 760px;
  margin-block-end: 2rem;
}

.feature-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.feature-card,
.role-card,
.service-card,
.gateway-card {
  border-radius: 12px;
}

.feature-card h3,
.architecture-flow h3 {
  font-size: 1.05rem;
  margin-block-end: 0.5rem;
}

.architecture-section {
  border-block: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.architecture-flow {
  display: grid;
  gap: 1rem;
  grid-template-columns: 1.1fr repeat(3, 1fr);
}

.gateway-card {
  background: rgba(var(--v-theme-primary), 0.08);
}

.role-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.role-card span {
  display: block;
  color: rgb(var(--v-theme-primary));
  font-size: 1.25rem;
  font-weight: 800;
  margin-block-end: 0.5rem;
}

.landing-cta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 18px;
  background: rgb(var(--v-theme-surface));
  gap: 2rem;
  margin-block: 4rem;
  padding-block: 2rem;
}

@media (max-width: 1100px) {
  .landing-hero,
  .problem-band,
  .architecture-flow {
    grid-template-columns: 1fr;
  }

  .feature-grid,
  .role-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .landing-nav {
    align-items: flex-start;
    gap: 1rem;
  }

  .landing-links {
    display: none;
  }

  .landing-hero {
    grid-template-columns: 1fr;
  }

  .hero-preview {
    min-inline-size: 0;
  }

  .feature-grid,
  .role-grid,
  .preview-grid,
  .problem-list {
    grid-template-columns: 1fr;
  }

  .landing-cta {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
