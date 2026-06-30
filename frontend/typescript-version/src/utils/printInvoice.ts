import { getOrderInvoice, type OrderInvoiceDto } from '@/services/orderSalesApi'

export async function printInvoice(orderId: string): Promise<void> {
  try {
    const invoice = await getOrderInvoice(orderId)
    triggerPrint(invoice)
  } catch (error) {
    console.error('Không thể lấy thông tin hóa đơn in:', error)
    alert('Không thể tải thông tin hóa đơn để in.')
  }
}

export function triggerPrint(invoice: OrderInvoiceDto): void {
  const printWindow = window.open('', '_blank', 'width=800,height=600')
  if (!printWindow) {
    alert('Vui lòng cho phép popup để in hóa đơn.')
    return
  }

  const formatCurrency = (val: number) =>
    new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(val)

  const formatDate = (dateStr: string) => {
    if (!dateStr) return '—'
    return new Date(dateStr).toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  const itemsHtml = invoice.items
    .map(
      (item, idx) => `
      <tr>
        <td style="padding: 4px 0; vertical-align: top;">${idx + 1}. ${item.productName}</td>
        <td style="padding: 4px 0; text-align: center; vertical-align: top;">${item.quantity}</td>
        <td style="padding: 4px 0; text-align: right; vertical-align: top;">${formatCurrency(item.unitPrice)}</td>
        <td style="padding: 4px 0; text-align: right; vertical-align: top; font-weight: bold;">${formatCurrency(item.subTotal)}</td>
      </tr>
    `
    )
    .join('')

  const promotionHtml = invoice.promotionDiscountAmount > 0
    ? `
      <div style="display: flex; justify-content: space-between; padding: 2px 0;">
        <span>Khuyến mãi:</span>
        <span style="font-weight: bold;">-${formatCurrency(invoice.promotionDiscountAmount)}</span>
      </div>
    `
    : ''

  const discountHtml = invoice.discountAmount > 0
    ? `
      <div style="display: flex; justify-content: space-between; padding: 2px 0;">
        <span>Chiết khấu (${invoice.discountPercent}%):</span>
        <span style="font-weight: bold;">-${formatCurrency(invoice.discountAmount)}</span>
      </div>
    `
    : ''

  const debtHtml = invoice.debtAmount > 0
    ? `
      <div style="display: flex; justify-content: space-between; padding: 2px 0; color: #ff5252;">
        <span>Ghi nợ công nợ:</span>
        <span style="font-weight: bold;">${formatCurrency(invoice.debtAmount)}</span>
      </div>
    `
    : ''

  const html = `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="utf-8">
      <title>In Hóa Đơn - ${invoice.orderCode}</title>
      <style>
        @page {
          size: 80mm auto;
          margin: 0;
        }
        body {
          font-family: 'Courier New', Courier, monospace, sans-serif;
          font-size: 12px;
          line-height: 1.4;
          color: #000;
          margin: 0;
          padding: 10px;
          width: 74mm;
        }
        .text-center { text-align: center; }
        .text-right { text-align: right; }
        .divider {
          border-top: 1px dashed #000;
          margin: 8px 0;
        }
        .header {
          margin-bottom: 12px;
        }
        .store-name {
          font-size: 16px;
          font-weight: bold;
          text-transform: uppercase;
        }
        .invoice-title {
          font-size: 14px;
          font-weight: bold;
          margin: 10px 0;
        }
        table {
          width: 100%;
          border-collapse: collapse;
        }
        th {
          border-bottom: 1px solid #000;
          padding: 4px 0;
          text-align: left;
        }
        .totals {
          margin-top: 8px;
        }
        .final-amount {
          font-size: 14px;
          font-weight: bold;
          border-top: 1px double #000;
          border-bottom: 1px double #000;
          padding: 4px 0;
          margin: 6px 0;
        }
        .footer {
          margin-top: 15px;
          font-size: 11px;
        }
        @media print {
          body {
            padding: 0;
            width: 80mm;
          }
          .no-print { display: none; }
        }
      </style>
    </head>
    <body>
      <div class="header text-center">
        <div class="store-name">RetailOps Supermarket</div>
        <div style="font-size: 11px;">Khu Công Nghệ Cao, Quận 9, TP. HCM</div>
        <div style="font-size: 11px;">Hotline: 1900 1234 - 0987 654 321</div>
        <div class="invoice-title">HÓA ĐƠN BÁN HÀNG</div>
        <div style="font-size: 11px; font-weight: bold;">Mã HD: ${invoice.orderCode}</div>
      </div>

      <div style="font-size: 11px;">
        <div>Ngày: ${formatDate(invoice.orderDate)}</div>
        <div>Thu ngân: ${invoice.createdByName}</div>
        <div>Khách hàng: ${invoice.customerName}</div>
        ${invoice.customerPhone ? `<div>SĐT: ${invoice.customerPhone}</div>` : ''}
        ${invoice.customerAddress ? `<div>Địa chỉ: ${invoice.customerAddress}</div>` : ''}
      </div>

      <div class="divider"></div>

      <table>
        <thead>
          <tr>
            <th style="width: 45%;">Tên SP</th>
            <th style="width: 10%; text-align: center;">SL</th>
            <th style="width: 20%; text-align: right;">Đơn giá</th>
            <th style="width: 25%; text-align: right;">T.Tiền</th>
          </tr>
        </thead>
        <tbody>
          ${itemsHtml}
        </tbody>
      </table>

      <div class="divider"></div>

      <div class="totals">
        <div style="display: flex; justify-content: space-between; padding: 2px 0;">
          <span>Tổng tiền hàng:</span>
          <span>${formatCurrency(invoice.subTotal)}</span>
        </div>
        ${promotionHtml}
        ${discountHtml}
        <div class="final-amount" style="display: flex; justify-content: space-between;">
          <span>THÀNH TIỀN:</span>
          <span>${formatCurrency(invoice.finalAmount)}</span>
        </div>
        <div style="display: flex; justify-content: space-between; padding: 2px 0;">
          <span>Thanh toán (${invoice.paymentMethod || 'Tiền mặt'}):</span>
          <span>${formatCurrency(invoice.paidAmount)}</span>
        </div>
        ${debtHtml}
      </div>

      <div class="divider"></div>

      <div class="footer text-center">
        <div style="font-weight: bold; font-style: italic; margin-bottom: 5px;">Cảm ơn quý khách. Hẹn gặp lại!</div>
        <div style="font-size: 10px; color: #555;">RetailOps - Hệ thống Quản lý Bán hàng & Tồn kho</div>
        <div style="margin-top: 10px;" class="no-print">
          <button onclick="window.print();" style="padding: 6px 16px; background: #2196f3; color: white; border: none; border-radius: 4px; cursor: pointer; font-family: sans-serif;">In hóa đơn</button>
        </div>
      </div>

      <script>
        window.onload = function() {
          setTimeout(function() {
            window.print();
          }, 300);
        };
      </script>
    </body>
    </html>
  `

  printWindow.document.open()
  printWindow.document.write(html)
  printWindow.document.close()
}
