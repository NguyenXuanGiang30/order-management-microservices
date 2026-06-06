export type Severity = 'success' | 'info' | 'warning' | 'error'

export interface MetricItem {
  label: string
  value: string
  helper: string
  icon: string
  color: Severity | 'primary' | 'secondary'
}

export interface ProductItem {
  sku: string
  barcode: string
  name: string
  category: string
  unit: string
  importPrice: number
  salePrice: number
  stock: number
  reserved: number
  minStock: number
  supplier: string
}

export interface OrderItem {
  code: string
  customer: string
  cashier: string
  total: number
  payment: string
  status: string
  time: string
}

export interface ReceiptItem {
  code: string
  supplier: string
  createdBy: string
  itemCount: number
  total: number
  status: string
  createdAt: string
}

export interface PartnerItem {
  code: string
  name: string
  phone: string
  group: string
  debt: number
  status: string
}

export interface PromotionItem {
  code: string
  name: string
  type: string
  value: string
  status: string
  activeUntil: string
}

export const products: ProductItem[] = [
  {
    sku: 'SKU-AT01',
    barcode: '8934673001121',
    name: 'Áo thun cổ tròn Basic - Đen',
    category: 'Thời trang',
    unit: 'cái',
    importPrice: 86000,
    salePrice: 159000,
    stock: 42,
    reserved: 4,
    minStock: 12,
    supplier: 'An Phát Textile',
  },
  {
    sku: 'SKU-QJ02',
    barcode: '8934673002319',
    name: 'Quần jean ống suông - Xanh nhạt',
    category: 'Thời trang',
    unit: 'cái',
    importPrice: 210000,
    salePrice: 399000,
    stock: 18,
    reserved: 2,
    minStock: 8,
    supplier: 'Việt Garment Co.',
  },
  {
    sku: 'SKU-GT42',
    barcode: '8934673004320',
    name: 'Giày thể thao Runner - Trắng',
    category: 'Giày dép',
    unit: 'đôi',
    importPrice: 640000,
    salePrice: 1290000,
    stock: 9,
    reserved: 1,
    minStock: 10,
    supplier: 'Global Shoes',
  },
  {
    sku: 'SKU-BP15',
    barcode: '8934673005556',
    name: 'Balo laptop 15 inch',
    category: 'Phụ kiện',
    unit: 'cái',
    importPrice: 295000,
    salePrice: 549000,
    stock: 61,
    reserved: 6,
    minStock: 15,
    supplier: 'Metro Supply',
  },
  {
    sku: 'SKU-HD88',
    barcode: '8934673007772',
    name: 'Tai nghe bluetooth HD88',
    category: 'Điện tử',
    unit: 'cái',
    importPrice: 190000,
    salePrice: 349000,
    stock: 5,
    reserved: 0,
    minStock: 10,
    supplier: 'Tech Link',
  },
]

export const orders: OrderItem[] = [
  {
    code: 'TXN-498210',
    customer: 'Lê Minh Anh',
    cashier: 'Thu ngân 01',
    total: 2048000,
    payment: 'Tiền mặt',
    status: 'Đã thanh toán',
    time: '09:24',
  },
  {
    code: 'TXN-498211',
    customer: 'Khách lẻ',
    cashier: 'Thu ngân 02',
    total: 549000,
    payment: 'Chuyển khoản',
    status: 'Đang chờ',
    time: '10:18',
  },
  {
    code: 'TXN-498212',
    customer: 'Nguyễn Văn A',
    cashier: 'Thu ngân 01',
    total: 1290000,
    payment: 'Ghi nợ',
    status: 'Ghi nợ',
    time: '11:03',
  },
  {
    code: 'TXN-498213',
    customer: 'Phạm Hoàng Linh',
    cashier: 'Thu ngân 03',
    total: 718000,
    payment: 'Tiền mặt',
    status: 'Đã trả',
    time: '13:58',
  },
]

export const receipts: ReceiptItem[] = [
  {
    code: 'GRN-1028',
    supplier: 'Global Shoes',
    createdBy: 'Thủ kho 01',
    itemCount: 12,
    total: 23560000,
    status: 'Bản nháp',
    createdAt: '30/05/2026 08:45',
  },
  {
    code: 'GRN-1027',
    supplier: 'Metro Supply',
    createdBy: 'Thủ kho 02',
    itemCount: 7,
    total: 16240000,
    status: 'Đã xác nhận',
    createdAt: '29/05/2026 16:10',
  },
  {
    code: 'GRN-1026',
    supplier: 'Tech Link',
    createdBy: 'Thủ kho 01',
    itemCount: 5,
    total: 8750000,
    status: 'Đã hủy',
    createdAt: '29/05/2026 10:32',
  },
]

export const customers: PartnerItem[] = [
  {
    code: 'CUS-001',
    name: 'Nguyễn Văn A',
    phone: '0902 456 150',
    group: 'VIP',
    debt: 2422500,
    status: 'Đang hoạt động',
  },
  {
    code: 'CUS-002',
    name: 'Lê Minh Anh',
    phone: '0918 221 889',
    group: 'Khách sỉ',
    debt: 0,
    status: 'Đang hoạt động',
  },
  {
    code: 'CUS-003',
    name: 'Phạm Hoàng Linh',
    phone: '0935 112 078',
    group: 'Khách lẻ',
    debt: 685000,
    status: 'Cần theo dõi',
  },
]

export const suppliers: PartnerItem[] = [
  {
    code: 'SUP-001',
    name: 'Global Shoes',
    phone: '028 6677 1020',
    group: 'Giày dép',
    debt: 18400000,
    status: 'Đang hoạt động',
  },
  {
    code: 'SUP-002',
    name: 'Metro Supply',
    phone: '024 8821 3400',
    group: 'Phụ kiện',
    debt: 4200000,
    status: 'Đang hoạt động',
  },
  {
    code: 'SUP-003',
    name: 'Tech Link',
    phone: '028 7750 2219',
    group: 'Điện tử',
    debt: 0,
    status: 'Tạm dừng',
  },
]

export const promotions: PromotionItem[] = [
  {
    code: 'SUMMER10',
    name: 'Giảm 10% đơn từ 500K',
    type: 'Đơn hàng',
    value: '10%',
    status: 'Đang chạy',
    activeUntil: '30/06/2026',
  },
  {
    code: 'COMBO-SHOES',
    name: 'Mua giày kèm phụ kiện',
    type: 'Combo',
    value: '120.000 đ',
    status: 'Đang chạy',
    activeUntil: '15/06/2026',
  },
  {
    code: 'OLD-STOCK',
    name: 'Xả hàng tồn cũ',
    type: 'Sản phẩm',
    value: '25%',
    status: 'Đã lên lịch',
    activeUntil: '01/07/2026',
  },
]

export const salesHours = [
  { hour: '08:00', value: 880000 },
  { hour: '09:00', value: 1540000 },
  { hour: '10:00', value: 2120000 },
  { hour: '11:00', value: 2480000 },
  { hour: '12:00', value: 1950000 },
  { hour: '13:00', value: 1220000 },
  { hour: '14:00', value: 1680000 },
  { hour: '15:00', value: 2240000 },
  { hour: '16:00', value: 2860000 },
  { hour: '17:00', value: 2320000 },
]

export const dashboardMetrics: MetricItem[] = [
  {
    label: 'Doanh thu hôm nay',
    value: '12.450.800 đ',
    helper: '+12.5% so với hôm qua',
    icon: 'ri-cash-line',
    color: 'primary',
  },
  {
    label: 'Tổng tồn kho',
    value: '4.892 đơn vị',
    helper: `${products.length} SKU đang theo dõi`,
    icon: 'ri-archive-line',
    color: 'info',
  },
  {
    label: 'Cần nhập hàng',
    value: '2 SKU',
    helper: 'Giày Runner và tai nghe HD88',
    icon: 'ri-alert-line',
    color: 'warning',
  },
  {
    label: 'Công nợ',
    value: '25.707.500 đ',
    helper: 'Khách hàng và nhà cung cấp',
    icon: 'ri-file-list-3-line',
    color: 'error',
  },
]

export const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

export const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

export const statusColor = (status: string): Severity => {
  if (['Đã thanh toán', 'Đã xác nhận', 'Đang hoạt động', 'Đang chạy', 'Tốt'].includes(status))
    return 'success'
  if (['Đang chờ', 'Bản nháp', 'Đã lên lịch', 'Cần theo dõi', 'Thấp'].includes(status))
    return 'warning'
  if (['Đã hủy', 'Đã trả', 'Ghi nợ', 'Tạm dừng', 'Nghiêm trọng'].includes(status))
    return 'error'

  return 'info'
}

export const stockStatus = (product: ProductItem) => {
  if (product.stock <= 0)
    return 'Hết hàng'
  if (product.stock <= product.minStock)
    return 'Thấp'
  if (product.stock > product.minStock * 4)
    return 'Tốt'

  return 'Bình thường'
}
