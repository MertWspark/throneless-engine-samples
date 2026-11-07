# Throneless — Motor Örnekleri

**Throneless: Ashes of the First**, Unity 6 ile geliştirdiğim voksel tabanlı bir aksiyon RPG. Hem PC hem de düşük donanımlı mobil cihazlarda çalışması hedefleniyor. Oyun geliştirme aşamasında ve kaynak kodu kapalı.

Bu depo, oyundaki iki sistemin **sadeleştirilmiş** sürümlerini içeriyor. Oyundaki asıl sürümler daha fazla özelliğe sahip; buradaki kodlar mimari yaklaşımımı göstermek için yeniden yazıldı.

## Ortak yaklaşım

- **Unity'den bağımsız çekirdek:** Hesaplama yapan kodlar `UnityEngine`'e bağlı değil. Unity tarafı sadece girdiyi verip sonucu çiziyor. Bu sayede çekirdek kod sahne kurmadan, saniyeler içinde test edilebiliyor.
- **Saf fonksiyonlar:** Aynı girdi her zaman aynı çıktıyı veriyor. Önizleme ile gerçek işlem aynı fonksiyonu kullanıyor, bu yüzden ekranda görülen ile oyunda olan hiçbir zaman ayrışmıyor.
- **Testle doğrulama:** Bulunan her hata için bir test yazılıyor. Örneğin içi boş kürenin kutuplarında delik açılması bir testle yakalandı ve düzeltildi.

## 1. İnşa şekil motoru (`Building/ShapeGenerator.cs`)

Oyuncu iki veya üç noktaya tıklayarak çizgi, duvar, zemin, kutu, çember, küre ya da kubbe inşa edebiliyor. Bu motor tıklanan noktalardan yerleştirilecek blokların listesini hesaplıyor.

```
Dolu çember (r=5)              İçi boş çember (r=5)
· · · ██████████· · ·          · · · ██████████· · ·
· · ██████████████· ·          · · ██· · · · · ██· ·
· ██████████████████·          · ██· · · · · · · ██·
██████████████████████         ██· · · · · · · · · ██
██████████████████████         ██· · · · · · · · · ██
██████████████████████         ██· · · · · · · · · ██
██████████████████████         ██· · · · · · · · · ██
██████████████████████         ██· · · · · · · · · ██
· ██████████████████·          · ██· · · · · · · ██·
· · ██████████████· ·          · · ██· · · · · ██· ·
· · · ██████████· · ·          · · · ██████████· · ·
```

Öne çıkan kararlar:
- **Eksen kilitleme:** Çizgi modunda fare biraz kaysa bile çizgi düz kalıyor; en uzun fark hangi eksendeyse sadece o eksen kullanılıyor.
- **Sadece tamsayı aritmetiği:** Çember ve çizgi hesaplarında karekök ya da ondalıklı sayı kullanılmıyor. Böylece farklı cihazlarda farklı şekil çıkmıyor.
- **Küre ve kubbe tek fonksiyon:** İkisi de yatay ve dikey yarıçapı ayrı olan bir elipsoid; kubbe sadece üst yarıyı çiziyor.

## 2. Adlandırılmış zamanlayıcılar (`Timing/NamedTimers.cs`)

Oyunda pek çok görsel geçiş aslında "bir değerin 0'dan 1'e gitmesi": zırhın kuşanılması, uçuş pozuna geçiş, bir efektin solması. Her biri için ayrı zamanlayıcı yazmak yerine hepsi bu tek sistemi kullanıyor.

```csharp
var timers = new NamedTimers();

// Oyuncu uçarken sayaç 0.4 saniyede 1'e çıkar, inince aynı sürede 0'a iner.
timers.Bind("flight_pose", 0.4f, () => player.IsFlying);

// Her karede:
timers.Tick(Time.deltaTime);
float blend = timers.Smoothed("flight_pose");   // animasyon bu değeri okur
```

Aynı sayaç hem açılışı hem kapanışı veriyor ve görsel taraf kendi durumunu tutmuyor; sadece değeri okuyor.

## Çalıştırma

.NET 8 SDK yeterli, harici paket gerekmiyor:

```bash
dotnet run --project tests/Throneless.Samples.Tests
```

## Notlar

- Geliştirmede yapay zeka destekli kodlama araçlarını kullanıyorum. Sistemlerin tasarımı, mimari kararlar ve test senaryoları bana ait.
- Bu depodaki kod yalnızca inceleme amaçlıdır; kullanım izni verilmemiştir (bkz. `LICENSE`).
