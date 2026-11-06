# Plan: "Something is Missing!" – greybox prototip

## Context
Boş Unity 6.3 URP şablonu (`Assets/` içinde sadece SampleScene + TutorialInfo, `activeInputHandler: 1` → sadece yeni Input System, Input System 1.20.1, uGUI 2.0 / TMP var ama TMP Essentials import edilmemiş). Unity MCP (AI Assistant) bağlı ve editör açık. Hedef: Wordle mantıklı ejderha besleme oyununun tamamen greybox, oyun mantığı görsellerden ayrık, model/animasyon sonradan kodsuz takılabilir ilk sürümü.

## Netleşen kararlar
- Raftaki malzemenin **kendisi** tabağa süzülür; tabakta aynı malzeme bir kez olabilir. Geri alınınca veya yendikten sonra rafa döner.
- Ortadan geri alınca **boşluk kalır**; sonraki seçim ilk boş slota gider.
- Feedback satırı: servis edilen malzemenin **mini görseli + yeşil/sarı/gri taban**, isim yok.
- Bitiş: kafa kameraya gelir → ekran kararır → TimeUp'ta doğru tarif (isimler), Final'de final metni; ikisinde restart ipucu.

## Varsayılanlar (itirazın varsa onayda söyle)
- Oyun içi metinler İngilizce, hepsi Inspector'dan değişir. Diyalog: yemek başı `Feed me.`, yanlış `Something is missing!`, doğru `Delicious... but I'm still hungry.`, süre bitti `Too slow. You'll do instead.`, final `Perfect... but something is still missing: YOU!`. Bitiş ekranı: `The recipe was:` + isimler (her biri kendi greybox renginde, küplerle eşleşsin diye), final `You were the missing ingredient.`, ipucu `Press R to restart`.
- Restart tuşu **R** (Inspector'daki InputAction'dan değişir), sadece TimeUp/Final durumunda çalışır.
- Kum saati servis/yeme animasyonu ve yemekler arası geçişte **durur** ("süre cezası yok" ile tutarlı).
- Yeni yemek başlayınca feedback satırı temizlenir. Yanlış serviste tarif aynı kalır.
- Tabakta tarif uzunluğu kadar küçük koyu slot işareti olur (kaç malzeme gerektiği görünsün diye).
- 10 malzeme: Kraken Tooth, Dragon Scale, Phoenix Feather, Basilisk Eye, Mandrake Root, Unicorn Horn, Griffin Egg, Troll Toenail, Banshee Tear, Hydra Tongue. Renkler: kırmızı, turuncu, kahverengi, pembe, bordo, magenta, mor, lacivert, mavi, camgöbeği (yeşil/sarı/gri tonu yok).
- Masa sahnede düz küp (prefab listende yoktu).

## Yapı
`Assets/_Project/` → `Scripts/{Data,Core,Input,View}`, `Prefabs/`, `Data/` (+`Data/Ingredients/`), `Materials/`, `Scenes/Main.unity`. Namespace yok, asmdef yok.

### Veri (ScriptableObject)
- `IngredientData`: `displayName`, `greyboxColor`, `modelPrefab` (opsiyonel).
- `GameConfig`: `ingredients` (List), `recipeLengths` (List<int> = 3,4,5), `dishDuration` = 120, `resolveDelay` (servis → sonuç), `nextDishDelay` (doğru → sonraki yemek/final). `OnValidate`: her uzunluk ≤ malzeme sayısı.

### Mantık (görselden bağımsız)
- `Recipe.cs`: `FeedbackType {Correct, Present, Absent}`, `ServeResult` (guess, feedback, IsCorrect), `Generate(pool, length)` (tekrarsız rastgele), `Evaluate(recipe, guess)` (Wordle iki geçiş).
- `GameController`: `GameState {Playing, Serving, DishTransition, TimeUp, Final}`, sayaç (sadece Playing'de akar), tabak `IngredientData[]` (null = boş slot). Komutlar: `TrySelect(data)`, `TryRemove(slot)`, `TryRestart()`. Okunur: `State`, `TimeNormalized`, `Recipe`, `DishIndex`.
  - C# event'ler: `DishStarted(index, length)`, `IngredientSelected(data, slot)`, `IngredientRemoved(data, slot)`, `Served(result)`, `DishWrong(result)`, `DishCorrect(result)`, `TimeUp(recipe)`, `Final()`, `Restarted()`. (`DishStarted` listende yoktu; tabak slotları ve kum saati için gerekli.)
  - Akış: tabak dolunca → Serving + `Served` → `resolveDelay` → `DishWrong` (Playing'e dön) / `DishCorrect` → `nextDishDelay` → sonraki `DishStarted` ya da son yemekse `Final`.
- `GameEventHooks`: aynı olayların parametresiz `UnityEvent` karşılıkları → Animator.SetTrigger, ses vb. Inspector'dan kodsuz bağlanır.

### Girdi
- `GameInput`: serialized `InputAction`'lar (Point `<Pointer>/position`, Click `<Pointer>/press`, Restart `<Keyboard>/r`). Tıklamada `ScreenPointToRay` + `Physics.Raycast` (yalnız "Interactable" layer) → `IngredientView` → rafta ise `TrySelect`, tabakta ise `TryRemove`.

### Görsel katman (sadece event dinler)
- `IngredientVisual` (static): `modelPrefab` varsa onu, yoksa `IngredientGreybox` küpünü `greyboxColor` ile boyayıp oluşturur (raf ve feedback ortak kullanır).
- `IngredientView`: data, collider garantisi (modelde yoksa bounds'a göre BoxCollider), Interactable layer, raf pozisyonu; `MoveTo` (süzülme), `ReturnHome`, `Consume` (küçülüp kaybolur), `Respawn` (rafta belirir).
- `Pantry`: config'teki malzemeleri oluşturur, sol/sağ rafa yarı yarıya böler, data → view eşlemesi tutar.
- `ShelfView` (Shelf.prefab, iki kez kullanılır): raf tahtalarının üstündeki satır anchor'ları + satır genişliği; malzemeleri satırlara eşit dağıtıp ortalar → sayı değişince otomatik dizilir. Model gelince anchor'lar modelin tahtalarına taşınır.
- `PlateView` (Plate.prefab): `DishStarted`'da uzunluk kadar slot pozisyonu + işaret; seçilen/geri alınan malzemeyi taşır; `Served` sonrası ısırma anında `Consume`, `DishWrong/DishCorrect`'te rafta `Respawn`; `Restarted`'da hepsi rafa.
- `DragonGreyboxAnimator` (Dragon.prefab: silindir boyun + küp kafa): `Served` → kafa tabağa uzanıp döner; `TimeUp/Final` → kısa bekleme sonrası kameraya gelir; boyun silindiri kafayı takip eder. Model + Animator gelince bu bileşen silinir, tetikler `GameEventHooks`'tan bağlanır.
- `HourglassView` (Hourglass.prefab): üst/alt kum objelerini `TimeNormalized` ile Y'de ölçekler (üst boşalır, alt dolar); `DishStarted`'da (ilk açılış hariç) 180° döner ve dolu hale gelir. Model gelince `body/topSand/bottomSand` referansları değiştirilir.
- `DialogView`: ejderhanın üstünde TextMeshPro, event başına metin.
- `IngredientRow` + `FeedbackView`: ejderhanın hemen altında; `DishWrong/DishCorrect`'te son servisi mini görsel + renkli taban (FeedbackTile.prefab, renkler Inspector'da) olarak çizer; `DishStarted/Restarted`'da temizler.
- `EndScreenView`: Screen Space Overlay canvas (siyah Image + CanvasGroup fade + TMP). TimeUp/Final'de gecikmeli kararır, ilgili metni gösterir; `Restarted`'da gizlenir.

### Sahne kurulumu (Unity MCP)
0. Onaydan sonra ilk iş: bu plan proje köküne `plan.md` olarak kaydedilir (Assets dışında, Unity import etmez). Bitirdiğim adımları orada işaretlerim.
1. TMP Essentials import (`TMP_PackageResourceImporter.ImportResources(true,false,false)` – `Unity_RunCommand`), "Interactable" layer (`Unity_ManageEditor AddLayer`).
2. Script'ler yazılır, derleme `Unity_GetConsoleLogs` ile kontrol.
3. `Unity_RunCommand` ile: URP Lit materyaller (koyu tonlar), 10 `IngredientData` + `GameConfig`, prefab'lar (Dragon, Plate, Hourglass, Shelf, IngredientGreybox, FeedbackTile, SlotMarker), `Main.unity`: siyah arka plan, düşük ambient, ejderha/masaya spot ışık, sabit kamera; masa, tabak (ön orta), kum saati (masa sağı), iki raf, `Game` objesi (GameController, GameEventHooks, GameInput, Pantry), diyalog/feedback/bitiş ekranı; referanslar bağlanır, Build Settings'e Main eklenir.
4. `Unity_Camera_Capture` ile kadraj kontrol edilip ayarlanır.

## Doğrulama
1. Console'da hata/uyarı yok.
2. `Unity_RunCommand`: `Evaluate` (ABC/ACB → G,Y,Y; ABC/ADE → G,gri,gri; ABC/ABC → hepsi G), `Generate` tekrarsız ve doğru uzunlukta.
3. Play mode + `Unity_RunCommand` ile akış: yanlış servis → `DishWrong`, tabak boş, Playing; doğru tarif → 4 ve 5 slotlu yemekler, kum saati döner; süreyi sıfıra çekince `TimeUp`; üç yemek çözülünce `Final`; `TryRestart` → `Restarted` + `DishStarted(0)`. Ara ara `Unity_Camera_Capture`.
4. Senin elle testin: tıklama, geri alma, süzülme, raf dizilimi.

## İlerleme
- [x] 0. plan.md
- [x] 1. TMP Essentials + Interactable layer (layer 8)
- [x] 2. Script'ler + derleme kontrolü
- [x] 3. Materyal / data / prefab / sahne (MCP)
- [x] 4. Kadraj ayarı (Camera_Capture render vermedi; kamera RenderTexture'a çizdirilerek kontrol edildi)
- [x] Doğrulama 1-3 (tıklama raycast'i 10/10; Input System üzerinden simüle tıklama editörde düşürüldü, sonuçsuz)
- [ ] Doğrulama 4: elle oyun testi (kullanıcı)

## Uygulama notları
- Statik `Recipe` sınıfıyla çakışmasın diye GameController'daki özellik adı `CurrentRecipe`.
- `IngredientRow` ayrı sınıf olmadı; tek kullanıcısı `FeedbackView` olduğu için içine alındı.
- Malzeme/feedback pivotu altta (raf/tabak anchor'ları yüzeyde durur).
- Kamera yönünde zayıf bir directional `FillLight` eklendi (boyun ve küplerin ön yüzleri tamamen kararıyordu).
- FeedbackRow ölçeği 1.25 (okunabilirlik).
- Build Settings: Main.unity açık, SampleScene listede ama kapalı.

---

# Diyalog görünümü (Inscryption hissi)

## Kararlar
- Font: `input folder/HEAVYWEI.TTF` → `Assets/_Project/Fonts/Heavyweight.ttf` + TMP SDF asset. Fontta `:` yok → final cümlesinde `-`.
- Tek satır: sabit küçük boyut (varsayılan 2.3), Inspector'da "satır yazı boyutu" olarak ayarlanır.
- Renkler: normal `#D8C9A8`, vurgu `#E0452B`. Vurgu yazımı `*kelime*`.
- Glow: her harf kendi renginde; yazının mesh'ini paylaşan arka katman + `M_DialogGlow`.
- Zıplama: her `.` `!` (`?`) sonunda o ana kadar görünen bütün yazı; `...` sadece duraksama.

## Yapı
- `DialogTypewriter` (DialogText üstünde): görünüm, açılma, duraksama, zıplama ayarları (AnimationCurve).
- `DialogView`: `typewriter.Play(line)` / `Clear()`.

## İlerleme
- [x] Font asset (`Assets/_Project/Fonts/Heavyweight SDF.asset`, 90pt / padding 12 / 2048x1024, statik)
- [x] DialogTypewriter + DialogView
- [x] Sahne: glow katmanı, materyal, referanslar
- [x] Doğrulama: 5 cümle tek satır (en uzun 3.93 / kutu 4), renkler, açılma, zıplama, `...` duraksaması
- [ ] Elle izleme (kullanıcı)

## Uygulama notları
- Glow, mesh paylaşımı yerine `DialogText/Glow` adlı ikinci TMP katmanı oldu: paylaşılan mesh'in harf kutuları dar kalıp glow'u köşeli kesiyordu. Typewriter iki katmana aynı açılma/zıplamayı uygular.
- Glow ayarları `M_DialogGlow`: Face alpha 0.2, Dilate 0.3, Softness 0.9.
- Proje Linear renk uzayında; TMP shader'ları vertex rengini çevirmediği için renkler kodda `.linear` ile yazılıyor (ekranda #D8C9A8 / #E0452B birebir).
- Heavyweight küçük harfleri küçük büyük harf (small caps) olarak çiziyor; fontun tasarımı.
- Cümle sonu: `.` `!` `?`; tekrarlanan işaretlerde sonuncusu sayılır.
- Revizyon 1 (kullanıcı): alttan yükselme kaldırıldı, harfler varsayılan olarak bir anda belirir (fade süresi ayarlı, 0 = anında). Yukarı zıplama "ucuz" bulundu → varsayılan kapalı (yükseklik 0). Cümle sonu efektleri: Ölçek vuruşu (miktar, süre, eğri, pivot: satır/harf) ve Titreme (şiddet, süre, sıklık, sönüm eğrisi, harf harf/blok).
- Editör arka plandayken play mode donuyor; MCP testlerinde oturumluk `Application.runInBackground = true` kullanıldı (proje ayarı değişmedi).
- Revizyon 2 (kullanıcı): "Idle Jitter" (durağan titreme: şiddet, sıklık, harf harf/blok) ve pikselleştirme eklendi. Pikselleştirme için `Assets/_Project/Shaders/DialogText.shader` (TMP Mobile SDF kopyası + `_PixelSize`, atlas texel'i cinsinden); ana yazı ve glow materyali bu shader'ı kullanır, değer `DialogTypewriter.pixelSize`'dan materyallere yazılır.

---

# MonsterAttackFeedback (ısırık anına çevresel tepkiler)

## Kararlar
- Tetik: `DragonGreyboxAnimator` → `onPlateBite` / `onPlayerBite` (kafa hedefe vardığı an) → `MonsterAttackFeedback.PlayPlateBite()` / `PlayPlayerBite()`. Model gelince animation event aynı metotları çağırır.
- İki ayar seti: Plate Bite, Player Bite (oyuncu yerken ayrı ayar). Player Bite'ta tabak sallanması ve particle varsayılan kapalı.
- Katmanlar (her biri aç/kapa, gecikme, süre, sönüm eğrisi): kamera sarsıntısı, FOV tepkisi, tabak sallanması, masa sarsıntısı, kum saati sarsıntısı, TableLight sallanması, particle patlaması.
- Başlangıç pozları/FOV efekt başlarken kaydedilir; efektler ofset olarak eklenir, bitince geri yüklenir.
- Particle: kameraya bakan çember düzleminde radyal, stretched billboard, unlit additive.
- TableLight'a yumuşak gölge. EndScreen gecikmesi 2.1 → 2.7.

## İlerleme
- [x] MonsterAttackFeedback + dragon olayları
- [x] Sahne: particle prefab/materyal, burst noktası, obje, bağlantılar, gölge, EndScreen gecikmesi
- [x] Doğrulama: gerçek servis ve süre bitimiyle iki ısırık; tüm pozlar/FOV birebir geri döndü; gölge ve particle görünüyor
- [ ] Elle izleme ve ince ayar (kullanıcı)

## Uygulama notları
- `BiteBurstPoint` Plate prefab'ına eklendi (tabak yüzeyinin 0.1 üstü). Particle: `Prefabs/BiteBurst` + `Materials/M_BiteBurst` (URP Particles/Unlit, additive).
- Revizyon (kullanıcı): particle ayarlarının tamamı BiteBurst prefab'ındaki ParticleSystem panelinde (Burst 24, Circle shape, start speed/size/lifetime/color); prefab modunda önizlenebilir. MonsterAttackFeedback sadece konumlar, kameraya çevirir (Circle yerel XY düzleminde) ve `Play` eder.
- Dragon prefab örneğindeki olay bağlantıları sahnede override olarak duruyor.
- Sarsıntılar Perlin gürültüsü, salınımlar sönen sinüs; aynı objeye gelen katmanlar toplanır.
- Revizyon (kullanıcı): patlama tabak düzleminde, tabak kenarından dışarı. BiteBurst Shape: Circle, Rotation (90,0,0), Radius 0.425 (= tabak yarıçapı), kenardan çıkış. BiteBurstPoint tabak yüzeyinde (yerel y 0.035, dönüşsüz). Plate Bite → Face Camera kapalı. Tabak boyutu değişirse Shape Radius elle güncellenmeli.

---

# Malzeme hover efekti

## Kararlar
- Raf ve tabaktaki malzemeler; sadece Playing durumunda.
- Hover'da: yukarı kalkma, sabit eğim (sol alta), kendi ekseninde yavaş dönüş; çıkınca yumuşak dönüş.
- Tek paylaşılan spot ışık; konumu malzemenin dinlenme pozuna göre ofset (Y yukarı, -Z önü), malzemenin merkezine bakar, gölgesiz, yumuşak açılıp kapanır.
- IngredientView: görsel merkezdeki `Pivot` altına; tıklama/hover için kökte sabit bounds collider (görsel collider'ları kaldırılır) → kenarda titreme olmaz.

## İlerleme
- [x] IngredientView pivot + sabit collider, GameInput hover, IngredientHoverFeedback
- [x] Sahne: IngredientHover objesi + HoverLight, bağlantılar
- [x] Doğrulama: hover pozu/ışık (merkeze tam bakıyor), çıkınca birebir dinlenme pozuna dönüş, raycast 10/10 (tek kök collider), tabakta hover
- [ ] Gerçek fareyle elle deneme (kullanıcı)
