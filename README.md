AuthApi Projesi – Dokümantasyon
1. Proje Genel Bilgileri

Proje Adı: AuthApi

Platform: .NET Core 9

Tip: Web API

Amaç: Kullanıcı ve yönetici yetkilendirmesi ile güvenli oturum yönetimi

2. Kullanılan Teknolojiler

JWT Authentication:
Kullanıcı girişinde JWT oluşturulur ve istemciye gönderilir. Token, sonraki isteklerde yetkilendirme için kullanılır.

ASP.NET Core Identity:
Kullanıcı ve yönetici yönetimi için kullanılır. Kayıt, giriş, şifre yönetimi ve roller Identity üzerinden kontrol edilir.

Role-Based Authorization:
Kullanıcı tipine göre endpoint erişimi kontrol edilir.
Örnek:

[Authorize(Roles = "Admin")]
[HttpGet("admin/data")]
public IActionResult GetAdminData() { ... }

[Authorize(Roles = "User")]
[HttpGet("user/data")]
public IActionResult GetUserData() { ... }


Database:
Kullanıcı ve rol bilgileri Entity Framework Core ile veritabanında tutulur. Identity standart tabloları kullanılır (AspNetUsers, AspNetRoles, AspNetUserRoles).

3. Controller Yapısı

AdminController: Yöneticiye özel operasyonlar

UserController: Normal kullanıcı işlemleri

4. Token Yönetimi

Giriş sırasında JWT oluşturulur.

Token süresi ve claim bilgileri belirlenir.

Middleware üzerinden doğrulama ve yetkilendirme sağlanır.

5. Kullanım Akışı

Kullanıcı /login endpoint’ine kimlik bilgileri ile istek gönderir.

Backend kimlik bilgilerini doğrular ve JWT üretir.

Kullanıcı token’ı alır ve sonraki isteklerde Authorization: Bearer <token> header’ı ile gönderir.

API middleware token’ı doğrular ve yetki kontrolünü yapar.

Kullanıcı rolüne göre ilgili endpoint’lere erişim sağlanır.

6. Örnek Endpointler
Endpoint	Yetki	Açıklama
/admin/data	Admin	Yöneticiye özel veri
/user/data	User	Kullanıcıya özel veri
/login	Public	Kullanıcı girişi ve JWT oluşturma
