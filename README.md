Genel Bilgi

Proje Adı: AuthApi

Platform: .NET Core 9

Tip: Web API

Amaç: Kullanıcı ve yönetici yetkilendirmesi ile güvenli oturum yönetimi

Kullanılan Teknolojiler ve Yapılar

JWT Authentication (Json Web Token)

Kullanıcı girişinde JWT oluşturulur ve istemciye gönderilir.

İstemci, sonraki isteklerde token’ı kullanarak yetkilendirilir.

Token ile stateless authentication sağlanır.

ASP.NET Core Identity

Kullanıcı ve yönetici (admin) yönetimi için kullanılır.

Kullanıcı kayıt, giriş, şifre yönetimi gibi işlemler Identity üzerinden yürütülür.

Rollere dayalı yetkilendirme uygulanır (User, Admin gibi).

Role-Based Authorization

API içinde endpoint’ler kullanıcı tipine göre korunur.

Örneğin:

[Authorize(Roles = "Admin")]
[HttpGet("admin/data")]
public IActionResult GetAdminData() { ... }

[Authorize(Roles = "User")]
[HttpGet("user/data")]
public IActionResult GetUserData() { ... }


Controller Yapısı

AdminController: Yöneticiye özel operasyonlar.

UserController: Normal kullanıcı işlemleri.

Token Management

Girişte JWT token oluşturulur, süresi ve claim’ler belirlenir.

Token doğrulama middleware üzerinden kontrol edilir.

Database

Kullanıcı ve rol bilgileri muhtemelen Entity Framework Core ile bir veritabanında tutulur.

Identity ile standart tablolar kullanılır (AspNetUsers, AspNetRoles, AspNetUserRoles vb.).

Örnek Kullanım Akışı

Kullanıcı /login endpoint’ine kimlik bilgileri ile istek gönderir.

Backend kimlik bilgilerini doğrular ve JWT üretir.

Kullanıcı token’ı alır ve sonraki isteklerde Authorization: Bearer <token> header’ı ile gönderir.

API middleware token’ı doğrular ve yetki kontrolünü yapar.

Kullanıcı rolüne göre ilgili endpoint’lere erişim sağlanır.
