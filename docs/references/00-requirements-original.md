# GỢI Ý ĐỀ TÀI BÀI TẬP LỚN

## Hạ tầng chung – áp dụng cho tất cả 7 đề tài
Tất cả 7 đề tài đều sử dụng chung một kiến trúc microservices chuẩn. Mỗi đề tài được phân chia thành 3 nhóm sinh viên, mỗi nhóm xây dựng và vận hành một service độc lập hoàn toàn. Các service giao tiếp với nhau thông qua API Gateway

| Thành phần | Công nghệ | Vai trò & Trách nhiệm |
| :--- | :--- | :--- |
| API Gateway | ASP.NET core API | Điểm vào duy nhất: xác thực JWT, định tuyến request tới đúng service, rate limiting |
| Frontend | VueJS 3 + Vuetify 3 | 1 project VueJS dùng chung, gọi API qua Gateway, quản lý state với Pinia |
| Database | SQL Server (mỗi service 1 DB riêng) | Database per service: service nào cần dữ liệu của service khác thì giao tiếp qua API hoặc message |
| Container | Docker + docker-compose | Mỗi service chạy trong container riêng, compose file chạy toàn bộ hệ thống |
| Auth | JWT Bearer Token | 1 service duy nhất cấp token, các service còn lại chỉ validate token bằng shared secret |
| API Docs | Swagger / OpenAPI | Mỗi service có Swagger Ul riêng, tài liệu hóa đầy đủ endpoint |

### Quy tắc thiết kế Microservice cho sinh viên
* Database per service: mỗi service có database SQL Server riêng, KHÔNG share database với service khác
* Không foreign key chéo service: thay vì FK cứng, lưu Id của bản ghi bên kia dưới dạng reference ID (plain column)
* Giao tiếp đồng bộ (REST): gọi API qua Gateway khi cần dữ liệu real-time, ví dụ Order Service gọi Product Service để lấy giá
* JWT do 1 service duy nhất cấp (User/Auth Service), các service còn lại chỉ validate token bằng shared secret
* Mỗi service deploy bằng Docker container độc lập, chạy được kể cả khi service khác bị down

---

## Danh sách 7 đề tài

| STT | Tên đề tài | Số vai trò | 3 Services (Nhóm 1 / Nhóm 2 / Nhóm 3) |
| :--- | :--- | :--- | :--- |
| 01 | Hệ thống quản lý bán hàng & kho hàng | 3 vai trò | Product & Inventory Service / Order & Sales Service / User & Report Service |
| 02 | Hệ thống quản lý thư viện số | 3 vai trò | Catalog Service / Circulation Service / Identity & Report Service |
| 03 | Hệ thống quản lý nhân sự & chấm công | 4 vai trò | HR Core Service / Attendance Service / Payroll & Report Service |
| 04 | Hệ thống quản lý khóa học & học viên trung tâm | 3 vai trò | Course & Schedule Service / Student & Attendance Service / Payment & Report Service |
| 05 | Hệ thống đặt lịch & quản lý phòng khám | 4 vai trò | Appointment Service / Medical Record Service / Pharmacy & Billing Service |
| 06 | Hệ thống quản lý dự án & phân công công việc | 3 vai trò | Project & Member Service / Task & Kanban Service / Comment & Notify Service |
| 07 | Hệ thống quản lý ký túc xá sinh viên | 3 vai trò | Room & Building Service / Contract & Student Service / Billing & Maintenance Service |

---

## ĐỀ TÀI 01: Hệ thống quản lý bán hàng & kho hàng

### 1. Mô tả đề tài
Nền tảng quản lý toàn bộ hoạt động bán lẻ nội bộ cho cửa hàng hoặc doanh nghiệp vừa và nhỏ theo mô hình microservices. Ba nhóm độc lập phụ trách: quản lý danh mục sản phẩm và kho, xử lý đơn hàng và giao dịch bản, quản lý người dùng và báo cáo tổng hợp.

### 2. Bài toán & lý do chọn Microservices
Cửa hàng vừa và nhỏ quản lý kho và bán hàng thủ công bằng Excel dễ dẫn đến sai lệch tồn kho, thiếu hàng đột xuất và khó theo dõi doanh thu. Việc tách thành microservices giúp mỗi nhóm phát triển và deploy độc lập, tăng khả năng mở rộng khi lượng đơn hàng tăng.

### 3. Phân quyền người dùng
* Admin: quản lý toàn hệ thống, xem báo cáo tổng hợp truy cập qua API Gateway
* Nhân viên bán hàng: tạo đơn bán, tra cứu sản phẩm JWT role Sales
* Thủ kho: quản lý nhập hàng, cập nhật tồn kho JWT role Warehouse

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Product & Inventory Service | Quản lý sản phẩm, danh mục, tồn kho, cảnh báo hết hàng, nhập kho từ nhà cung cấp |
| Nhóm 2 - Order & Sales Service | Xử lý đơn bán hàng, quản lý khách hàng, nhà cung cấp, công nợ |
| Nhóm 3 - User & Report Service | JWT auth, quản lý tài khoản, báo cáo doanh thu và thống kê tổng hợp |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng. Docker container độc lập. Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Product & Inventory Service | ProductDB | - CRUD sản phẩm: mã, tên, giá nhập, giá bán, ảnh, thể loại <br> - Phân loại theo danh mục cha-con (self-referencing) <br> - Theo dõi tồn kho, cảnh báo khi tồn kho < ngưỡng tối thiểu <br> - Phụ trách nhập kho: tạo phiếu nhập, xác nhận nhập, cập nhật tồn kho <br> - Publish event stock.updated khi tồn kho thay đổi |
| N2 | Order & Sales Service | OrderDB | - Tạo đơn bán hàng, tính tổng tiền, áp dụng chiết khấu <br> - Quản lý khách hàng: thông tin, lịch sử mua, công nợ <br> - Quản lý nhà cung cấp và thông tin liên hệ <br> - Consume stock.updated để kiểm tra sản phẩm còn hàng <br> - Publish event order.created sau khi đặt hàng thành công |
| N3 | User & Report Service | UserDB | - JWT login, quản lý tài khoản và phân quyền 3 vai trò <br> - Dashboard: doanh thu hôm nay/tuần/tháng, số đơn hàng <br> - Top 10 sản phẩm bán chạy, top khách hàng theo doanh số <br> - Consume order created để tổng hợp báo cáo doanh thu <br> - Biểu đồ doanh thu theo ngày/tháng bằng Chart.js |

---

## ĐỀ TÀI 02: Hệ thống quản lý thư viện số

### 1. Mô tả đề tài
Ứng dụng quản lý thư viện trường học theo kiến trúc microservices với ba service độc lập: Catalog Service quản lý đầu sách và số bản sao, Circulation Service xử lý mượn/trả và tính phạt, Identity Service quản lý tài khoản độc giả và thống kê. Sinh viên học cách tách nghiệp vụ thư viện thành các domain riêng biệt.

### 2. Bài toán & lý do chọn Microservices
Thư viện truyền thống gặp khó khăn khi số lượng độc giả lớn: tìm kiếm sách chậm, theo dõi mượn/trả thủ công, tính phạt sai. Kiến trúc microservices cho phép mở rộng riêng từng service ví dụ scale Catalog Service khi nhu cầu tìm kiếm tăng cao.

### 3. Phân quyền người dùng
* Admin: quản lý danh mục sách, cấu hình quy tắc mượn trả, xem báo cáo tổng hợp
* Thủ thư: xử lý phiếu mượn/trả, tra cứu tình trạng, xác nhận trả sách
* Độc giả: tìm kiếm sách, xem lịch sử mượn, trạng thái phiếu mượn hiện tại

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Catalog Service | CRUD đầu sách, số bản sao, tìm kiếm, trạng thái có thể mượn |
| Nhóm 2 - Circulation Service | Xử lý mượn/trả sách, tính phí phạt quả hạn, quản lý công nợ |
| Nhóm 3 - Identity & Report Service | JWT login, hồ sơ độc giả, thẻ thư viện, thống kê mượn trả |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng. Docker container độc lập Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Catalog Service | CatalogDB | - CRUD sách: ISBN, tiêu đề, tác giả, NXB, năm XB, ảnh bìa <br> - Quản lý số lượng bản sao, tình trạng sách (có thể mượn/hết) <br> - Tìm kiếm sách theo tên, tác giả, thể loại, năm xuất bản <br> - Publish event book.availability.changed khi số lượng thay đổi |
| N2 | Circulation Service | CirculationDB | - Tạo phiếu mượn, kiểm tra giới hạn mượn của độc giả <br> - Xử lý trả sách, tự động tỉnh phạt theo ngày quá hạn <br> - Quản lý công nợ phí phạt, danh sách sách quả hạn <br> - Publish event book.borrowed / book.returned |
| N3 | Identity & Report Service | IdentityDB | - JWT login, đăng ký tài khoản độc giả mới <br> - Hồ sơ độc giả: thẻ thư viện, ngày hết hạn, trạng thái khóa/mở <br> - Dashboard: thống kê mượn trả, top sách phổ biển <br> - Consume events để tổng hợp báo cáo |

---

## ĐỀ TÀI 03: Hệ thống quản lý nhân sự & chấm công

### 1. Mô tả đề tài
Hệ thống HRM (Human Resource Management) theo kiến trúc microservices cho doanh nghiệp vừa và nhỏ. Ba service phụ trách: hồ sơ nhân viên và phòng ban, chấm công và nghỉ phép, tỉnh lương và báo cáo. Sinh viên học cách thiết kế domain HR phức tạp với nhiều logic nghiệp vụ.

### 2. Bài toán & lý do chọn Microservices
Doanh nghiệp vừa và nhỏ quản lý nhân sự bằng Excel gây sai lệch chấm công, tính lương thiếu minh bạch và khó theo dõi nghỉ phép. Tách thành microservices giúp HR Service, Attendance Service và Payroll Service hoạt động độc lập Payroll Service có thể chạy batch tính lương cuối tháng mà không ảnh hưởng tới service khác.

### 3. Phân quyền người dùng
* Admin/HR: toàn quyền quản lý nhân sự, chạy tính lương, xem báo cáo
* Manager: duyệt nghỉ phép nhân viên trong phòng ban, xem báo cáo phòng
* Nhân viên: chấm công cá nhân, đăng ký nghỉ phép, xem bảng lương của mình

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - HR Core Service | Hồ sơ nhân viên, phòng ban, chức vụ, cơ cấu tổ chức dạng cây |
| Nhóm 2 - Attendance Service | Chấm công hàng ngày, ca làm việc, đăng ký và duyệt nghỉ phép |
| Nhóm 3 - Payroll & Report Service | Cấu hình lương, tính lương tự động, báo cáo nhân sự |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng Docker container độc lập Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | HR Core Service | HRCoreDB | - CRUD nhân viên: mã NV, họ tên, phòng ban, chức vụ, ngày vào làm <br> - Quản lý phòng ban dạng cây (cha-con, self-referencing) <br> - Phân loại hợp đồng: chính thức, thử việc, part-time <br> - Publish event employee.created/employee.updated |
| N2 | Attendance Service | Attendance DB | - Chấm công vào/ra hàng ngày, tỉnh giờ tăng ca <br> - Quản lý ca làm việc: sáng, chiều, đêm với giờ bắt đầu kết thúc <br> - Đăng ký & duyệt nghỉ phép theo workflow Pending Approved Publish event attendance.monthly.closed cuối tháng |
| N3 | Payroll & Report Service | PayrollDB | - Cấu hình lương: cơ bản, phụ cấp ăn, xe, bảo hiểm, khấu trừ <br> - Consume event cuối tháng, tính lương tự động cho tất cả NV <br> - Bảng lương chi tiết, báo cáo tổng hợp theo phòng ban <br> - Dashboard HR: số NV, tổng quỹ lương, biểu đồ |

---

## ĐỀ TÀI 04: Hệ thống quản lý khóa học & học viên trung tâm

### 1. Mô tả đề tài
Nền tảng quản lý trung tâm đào tạo (ngoại ngữ, tin học, kỹ năng) theo kiến trúc microservices. Ba service: quản lý khóa học và lịch học, quản lý học viên và điểm danh, quản lý học phí và kết quả. Sinh viên học cách tách nghiệp vụ giáo dục thành các bounded context rõ ràng.

### 2. Bài toán & lý do chọn Microservices
Trung tâm đào tạo với hàng trăm học viên không thể quản lý thủ công: sắp xếp lớp, điểm danh từng buổi, thu học phí và thông báo kết quả mất nhiều công sức. Kiến trúc microservices cho phép mỗi team phát triển độc lập từng phần của hệ thống.

### 3. Phân quyền người dùng
* Admin: quản lý toàn bộ khóa học, giáo viên, báo cáo tài chính
* Giáo viên: xem lớp mình phụ trách, điểm danh, nhập kết quả kiểm tra
* Học viên: xem lịch học, tỷ lệ chuyên cần, kết quả và học phí của mình

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Course & Schedule Service | CRUD khóa học, mở lớp học, quản lý lịch học và sĩ số |
| Nhóm 2 - Student & Attendance Service | Hồ sơ học viên, đăng ký khóa học, điểm danh, kết quả |
| Nhóm 3 - Payment & Report Service | JWT auth, quản lý học phí, công nợ và báo cáo doanh thu |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng. Docker container độc lập. Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Course & Schedule Service | CourseDB | - CRUD khóa học: tên, học phí, số buổi, trình độ, mô tả <br> - Mở lớp học: giáo viên phụ trách, phòng học, Sĩ số tối đa <br> - Cấu hình lịch học: thứ giờ trong tuần, buổi sáng/chiều/tối <br> - Publish event class.opened khi mở lớp mới |
| N2 | Student & Attendance Service | StudentDB | - Hồ sơ học viên: thông tin cá nhân, khóa học đã đang học <br> - Đăng ký khóa học, quản lý trạng thái đang học/hoàn thành <br> - Điểm danh theo từng buổi học, tính % chuyên cần <br> - Nhập điểm kiểm tra giữa kỳ và cuối kỳ, tính kết quả |
| N3 | Payment & Report Service | PaymentDB | - JWT login, quản lý tài khoản 3 vai trò <br> - Tạo phiếu thu học phí, ghi nhận thanh toán <br> - Theo dõi công nợ học phí từng học viên <br> - Dashboard doanh thu, báo cáo theo khóa học/lớp |

---

## ĐỀ TÀI 05: Hệ thống đặt lịch & quản lý phòng khám

### 1. Mô tả đề tài
Hệ thống quản lý phòng khám đa khoa quy mô vừa theo kiến trúc microservices. Ba service: quản lý bác sĩ và lịch hẹn, quản lý khám bệnh và bệnh án điện tử, quản lý kho thuốc và viện phí. Sinh viên học cách xây dựng domain y tế phức tạp với 4 vai trò người dùng.

### 2. Bài toán & lý do chọn Microservices
Phòng khám nhỏ xếp lịch thủ công dễ trùng giờ, hồ sơ bệnh án giấy khó tra cứu. Với microservices, Appointment Service có thể scale riêng trong giờ cao điểm đặt lịch, còn Medical Record Service chạy ổn định phục vụ bác sĩ tra cứu bệnh án.

### 3. Phân quyền người dùng
* Admin: quản lý bác sĩ, danh mục thuốc, cấu hình hệ thống, báo cáo tài chính
* Bác sĩ: xem lịch hẹn hôm nay, khám bệnh, ghi bệnh án, kê đơn thuốc
* Y tá/Tiếp tân: tiếp nhận bệnh nhân, xếp hàng chờ, thu viện phí
* Bệnh nhân: đặt lịch khám, xem lịch sử khám, đơn thuốc cũ

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Appointment Service | Hồ sơ bác sĩ, đặt lịch khám, xếp hàng chờ bệnh nhân |
| Nhóm 2 - Medical Record Service | Hồ sơ bệnh nhân, bệnh án điện tử, kê đơn thuốc |
| Nhóm 3 - Pharmacy & Billing Service | JWT auth 4 vai trò, kho thuốc, tính và thu viện phí |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng Docker container độc lập Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Appointment Service | AppointmentDB | - Hồ sơ bác sĩ: chuyên khoa, bằng cấp, lịch làm việc, phí khám <br> - Bệnh nhân đặt lịch: chọn chuyên khoa bác sĩ ngày/giờ còn trống <br> - Phát hiện conflict slot, không cho đặt trùng giờ <br> - Tiếp tân xác nhận, thêm vào hàng chờ, hiển thị số thứ tự |
| N2 | Medical Record Service | MedicalDB | - Hồ sơ bệnh nhân: thông tin cá nhân, tiền sử bệnh, dị ứng <br> - Ghi bệnh án: triệu chứng, chẩn đoán, ghi chú của bác sĩ <br> - Kê đơn thuốc theo bệnh án, chọn thuốc từ danh mục <br> - Publish event prescription.created sau khi kê đơn |
| N3 | Pharmacy & Billing Service | PharmacyDB | - JWT login, quản lý tài khoản 4 vai trò <br> - Quản lý kho thuốc: tên, hoạt chất, đơn vị, giá, tồn kho <br> - Consume prescription.created, xuất thuốc từ kho <br> - Tính và thu viện phí (phí khám + thuốc theo don) |

---

## ĐỀ TÀI 06: Hệ thống quản lý dự án & phân công công việc

### 1. Mô tả đề tài
Công cụ quản lý dự án và task nội bộ theo mô hình Kanban/Scrum đơn giản với kiến trúc microservices. Ba service: quản lý dự án và thành viên, quản lý task và kanban board, quản lý bình luận và thông báo. Sinh viên học cách thiết kế hệ thống cộng tác theo domain.

### 2. Bài toán & lý do chọn Microservices
Nhóm làm việc không có công cụ tập trung dẫn đến không biết ai đang làm gì, deadline nào sắp đến. Kiến trúc microservices cho phép tách biệt rõ ràng: Project Service quản lý quyền truy cập, Task Service xử lý logic Kanban, Notification Service phát thông báo.

### 3. Phân quyền người dùng
* Project Manager: tạo và quản lý dự án, thêm thành viên, xem toàn bộ tiến độ
* Member/Developer: xem task được giao, cập nhật trạng thái, bình luận, log thời gian
* Viewer: xem tiến độ dự án nhưng không chỉnh sửa task

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Project & Member Service | Tạo dự án, quản lý thành viên với vai trò, sprint và milestone |
| Nhóm 2 - Task & Kanban Service | CRUD task, kanban board, sub-task, log thời gian làm việc |
| Nhóm 3 - Comment & Notify Service | JWT auth, bình luận vào task, nhật ký hoạt động, thông báo |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng. Docker container độc lập. Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Project & Member Service | ProjectDB | - Tạo dự án: tên, mô tả, ngày bắt đầu kết thúc, màu nhận diện <br> - Quản lý thành viên: Owner / Manager / Member / Viewer <br> - Tạo sprint với mục tiêu và thời gian chạy 2 tuần <br> - Publish event project.member.added / sprint.started |
| N2 | Task & Kanban Service | TaskDB | - Tạo task: tiêu đề, mô tả, độ ưu tiên, nhăn màu, deadline <br> - Kanban board: Backlog To Do → In Progress → Review → Done <br> - Sub-task, gán người phụ trách, log giờ làm việc <br> - Publish event task.status.changed / task.assigned |
| N3 | Comment & Notify Service | NotifyDB | - JWT login, quản lý tài khoản người dùng <br> - Bình luận vào task, nhật ký hoạt động tự động <br> - Consume events để gửi thông báo trong app <br> - Lưu trữ notification, đánh dấu đã đọc |

---

## ĐỀ TÀI 07: Hệ thống quản lý ký túc xá sinh viên

### 1. Mô tả đề tài
Ứng dụng quản lý ký túc xá trường đại học theo kiến trúc microservices. Ba service phụ trách: quản lý tòa nhà và phòng ở, xử lý hợp đồng và đăng ký phòng, quản lý thu tiền và xử lý sự cố. Sinh viên học cách tách hệ thống quản lý tài sản thành các service có trách nhiệm rõ ràng.

### 2. Bài toán & lý do chọn Microservices
Ký túc xá trường học với hàng nghìn sinh viên không thể quản lý phòng, hợp đồng và thu tiền bằng Excel. Tách thành microservices giúp nhân viên ký túc xá, kế toán và ban quản lý làm việc trên các phần hệ thống riêng biệt mà không ảnh hưởng lẫn nhau.

### 3. Phân quyền người dùng
* Quản lý KTX (Admin): toàn quyền quản lý tòa nhà, phòng, hợp đồng, báo cáo tài chính
* Nhân viên: xử lý đăng ký phòng, thu tiền, xử lý yêu cầu sửa chữa
* Sinh viên: đăng ký phòng, xem hợp đồng, trạng thái thanh toán, gửi sự cố

### 4. Kiến trúc Microservices tổng quan

| Thành phần | Chức năng |
| :--- | :--- |
| API Gateway | Ocelot API Gateway điểm vào duy nhất, định tuyến request tới đúng service, xác thực JWT trước khi forward |
| Nhóm 1 - Room & Building Service | CRUD tòa nhà, loại phòng, trạng thái phòng, sơ đồ phòng theo tầng |
| Nhóm 2 - Contract & Student Service | Hồ sơ sinh viên, đăng ký phòng online, hợp đồng thuê phòng |
| Nhóm 3 - Billing & Maintenance Service | JWT auth, thu tiền phòng hàng tháng, xử lý yêu cầu sửa chữa |

Hạ tầng bổ sung: Mỗi nhóm: 1 ASP.NET Core 8 Web API 1 SQL Server database riêng. Docker container độc lập. Swagger UI

### 5. Phân công 3 nhóm Service & Database

| Nhóm | Tên Service | Database | Chức năng chính |
| :--- | :--- | :--- | :--- |
| N1 | Room & Building Service | RoomDB | - CRUD tòa nhà: tên, số tầng, loại KTX (nam/nữ hỗn hợp) <br> - Quản lý loại phòng: sức chứa, giá thuê/tháng, tiện nghi <br> - Trạng thái phòng: còn chỗ, đầy, bảo trì; sơ đồ phòng theo tầng <br> - Publish event room.status.changed khi phòng thay đổi trạng thái |
| N2 | Contract & Student Service | ContractDB | - Hồ sơ sinh viên: mã SV, họ tên, khoa, liên hệ <br> - Đăng ký phòng online: chọn tòa, loại phòng, thời gian ở <br> - Admin duyệt đơn, xếp phòng cụ thể, tạo hợp đồng <br> - Publish event contract.created/ contract.expired |
| N3 | Billing & Maintenance Service | Billing DB | - JWT login, quản lý tài khoản 3 vai trò <br> - Tự động tạo phiếu thu tiền hàng tháng theo hợp đồng <br> - Nhận thanh toán, theo dõi công nợ sinh viên <br> - Xử lý yêu cầu sửa chữa: Mới -> Đang xử lý -> Hoàn thành -> Ghi nhận |
