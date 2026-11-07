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
- IngredientView: görsel merkezdeki `Pivot` altına; tıklama/hover için kökte sabit bounds collider (görsel collider'ları kaldırılır) → kenarda titreme olmaz.

## İlerleme
- [x] IngredientView pivot + sabit collider, GameInput hover, IngredientHoverFeedback
- [x] Sahne: IngredientHover objesi + HoverLight, bağlantılar
- [x] Doğrulama: hover pozu/ışık (merkeze tam bakıyor), çıkınca birebir dinlenme pozuna dönüş, raycast 10/10 (tek kök collider), tabakta hover
- [ ] Gerçek fareyle elle deneme (kullanıcı)

## Revizyon: ışık ve glow kaldırıldı (kullanıcı beğenmedi)
- Hover sadece hareket: kalkma, eğim, kendi ekseninde dönüş. Spot ışık (commit 86d5423'te duruyor) ve denenen yumuşak hale silindi; hale dosyaları (M_HoverGlow, T_SoftGlow) hiç commit'lenmedi.

---

# Blender sahnesi (Mekan_2.fbx)

## Kararlar
- Mekan_2 orijinde, model konumları Blender'daki gibi. Greybox masa/raflar sahneden kalkar (prefab dosyaları kalır). Eski Mekan.fbx silinir.
- Kamera: Blender kamerasının pozu + FOV 63 → Main Camera. FBX kamerası import edilmez.
- Işık: sadece Blender spotu (güç elle, yumuşak gölge, ısırıkta sallanır). Unity'deki 5 ışık kalkar.
- Malzemeler: IngredientData.modelPrefab = FBX'teki ing# (başlangıç sırayla). Rafta o modelin Blender konumunda durur; FBX'teki ing objeleri gizli konum işareti. Texture gelene kadar modeller greyboxColor ile boyanır (IngredientData.tintModel).
- Tabak: yuva işaretleri yok, malzemeler 0.75 ölçekte, soldan sağa sıralı; model tabanı tabak yüzeyine oturur.
- Feedback minyatürleri karoya sığacak şekilde otomatik ölçeklenir ve tabana oturur.
- Ejderha greybox, yeni ölçeğe büyütülür, masanın arkasında. Diyalog/feedback/ısırık noktaları/burst yarıçapı yeni sahneye göre.
- Kum saati: model görünür, kum göstergesi geçici olarak yok (cam ayrı materyal + tek kum objesi gelince shader ile).

## Blender'dan beklenen (kum saati)
- Cam ayrı materyal slotu.
- Kum: iki hazneyi dolduran tek obje, origin boğazın ortasında.

## İlerleme
- [x] Kod: model yerleşimi/boyama, tabakta ölçek + oturtma, minyatür sığdırma, kum saati kumsuz çalışma, tabak yuvaları çanak yüzeyine oturur (PlateView.surface)
- [x] Sahne: Mekan_2, kamera, ışık, plate/hourglass prefab modelleri, ejderha, diyalog/feedback, referanslar
- [x] Doğrulama: 10/10 malzeme ekranda ve tıklanabilir, tabakta 0.75 + yüzeye oturuyor, ısırık hedefe gidiyor, feedback minyatürleri karoya oturuyor
- [ ] Kum saati kumu (Blender parçaları bekleniyor) + elle deneme (kullanıcı)

## Uygulama notları
- Işık gücü 20 (Blender'dan 1426 geldi). Raflar loş; ayar Mekan/Light'ta.
- Tabak: kök çanağın merkez yüzeyinde (y 1.698); Mesh'te MeshCollider (yuvalar için yüzey). Yuva aralığı 0.2; geniş modeller (ing2) komşusuna ~4 cm değebiliyor.
- Kum saati prefab'ı: Body/Model (döndürme merkezi), kum referansları boş.
- Ejderha: kafa (0, 2.7, 1.6) ölçek (1.1, 0.95, 1.2), boyun 0.58 kalın, boyun dibi masanın arkasında. PlayerTarget kameranın 0.8 önünde (kafa near plane'e girmesin).
- Diyalog (0, 3.6, 1.6) ölçek 1.5; FeedbackRow (0, 1.95, 1.2) ölçek 1.9.
- Tabak ilk yerleşimde Blender'daki `ortatabak` ile birebirdi; Blender'da masanın çukur yüzeyinden (y 1.399) ~20 cm yukarıdaydı. Kullanıcı Plate kökünü 1.503'e indirdi (taban masanın ~1 cm üstünde).
- Kum saati de Blender konumunda; masanın yüzeyinden 14 cm yukarıda (kullanıcıya soruldu).
- DragonGreyboxAnimator [ExecuteAlways]: editörde sadece boynu kafaya bağlı tutar; olay aboneliği/animasyon yalnız Play'de.
- Işık gücü kullanıcı isteğiyle Blender değerine (1426.86) döndü; sahne patlıyor, ton eşleme/ortam ışığı için Blender ayarları bekleniyor.

## Mekan_3 (masa ve kum saati düzeltildi)
- FBX yerinde güncellendi: Mekan_2.fbx → Mekan_3.fbx (aynı GUID, bağlantılar korundu).
- Masa artık `Cube` (üst yüz y 1.653, yüzler doğru). MonsterAttackFeedback.table → Cube. Gelecekteki otomatik araç için Blender'da `Masa` adı önerildi.
- Tabak Blender konumuna birebir geri alındı (kök y 1.6983, ortatabak 1.7571).
- Kum saati: hourglass-frame / -up / -down. Prefab Body (boğazda, döndürme merkezi): Frame, GlassTop/GlassBottom (M_HourglassGlass, yarı saydam Lit), SandTop/SandBottom (haznelerin %90 kopyası, M_HourglassSand).
- `Shaders/HourglassSand.shader`: tek pass, Forward+ ışık döngüsü, half-Lambert; `_SandPlane` üstü kesilir, arka yüzler düz kum yüzeyi olarak çizilir. HourglassView kesme düzlemini Body ile birlikte döndürür (ters çevirmede kum camla döner).
- [x] Doğrulama: %92 / %41 kalan sürede seviye doğru, ters dönme ortası ve sonu doğru, masa üst yüzü görünüyor.

---

# Işıklar + volumetrik

## Kararlar
- 4 ışık: tepe (Blender spotu, mevcut), ejderha (sıcak mum rengi), sol ve sağ dolap.
- Ton eşleme: post-processing açılır, global Volume + Tonemapping.
- Volumetrik: CristianQiu/Unity-URP-Volumetric-Light (MIT, git paketi). Dört ışıkta da açık; her ışıktaki VolumetricAdditionalLight bileşeni kapatılarak tek tek iptal edilebilir.
- Renderer: volumetric fog renderer feature, Transparent Receive Shadows açık.
- Risk: paket Metal/macOS'ta resmi olarak test edilmemiş; editörde görüntüyle doğrulanacak.

## İlerleme
- [x] Paket kurulumu (kullanıcı tek seferlik izin verdi): com.cqf.urpvolumetricfog 0.5.91, Packages/manifest.json
- [x] Kamera post-processing + Global Volume (`Rendering/VP_Main`, Tonemapping Neutral)
- [x] PC_Renderer'a VolumetricFog özelliği; VP_Main'e Volumetric Fog (enabled, additional lights açık, main light kapalı, distance 20, max height 6.5, density 0.07)
- [x] Işıklar: `Lights/DragonLight` (sıcak mum, 12), `ShelfLightL/R` (8), tepe ışığı Mekan/Light (kullanıcı ayarı 50)
- [x] 4 ışıkta VolumetricAdditionalLight (scattering: tepe 1, ejderha 0.6, dolaplar 0.35). Bileşen kapatılınca o ışığın huzmesi gider.
- [x] Mac/Metal'de çalıştığı görüntüyle doğrulandı. Paketin shader'ında zararsız bir uyarı var (_FORWARD_PLUS keyword'ü eskimiş).
- Revizyon (kullanıcı): ışıklar nötr-soğuk (#EEF2FF, dördü de; tepe ışığı Mekan/Light'ta override). Siyahlar koyu laciverte: VP_Main → Lift Gamma Gain, Lift (0.997, 1, 1.006, offset 0.0015). En karanlık bölge (2,3,3) → (2,3,15).

---

# Performans (hedef sabit 60 FPS)

## Ölçüm (editörde, kamera render + GPU senkronu; editör yükü ~10 ms dahil, gürültülü)
- Önce: 1080p ~17-20 ms, 2880x1620 ~22 ms. En büyük yükler: volumetrik sis (5-8 ms), SSAO (3-6 ms).
- Sonra: 1080p ~13-16 ms, 2880x1620 ~16-18 ms. Görüntü farkı fark edilmiyor.

## Yapılanlar
- SSAO renderer feature kapalı (karanlık sahnede görünmüyordu; PC_Renderer'dan tek tikle açılır).
- Camera Opaque Texture kapalı (hiçbir shader kullanmıyor).
- Volumetric Fog: Max Steps 128 → 48, Blur 2 → 1, Distance 20 → 14. Sadece tepe ışığında volumetrik (kullanıcı kapattı).
- FrameRateCap (Game objesi): VSync kapalı, hedef 60 FPS.
- Kod: her karede bellek ayıran iki lambda kaldırıldı (DialogTypewriter.LastSentenceEnd, MonsterAttackFeedback.IsRunning). Derleyici uyarısı yok.
- Kullanılmayan greybox dosyaları silindi: Shelf.prefab, SlotMarker.prefab, M_Metal, M_Plate, M_Sand, ardından boşa düşen M_SlotMarker, M_Wood. Kalan her prefab/materyal bir yerde kullanılıyor.
- Tepe ışığı `Lights/TopLight` oldu (Mekan'daki ışığın birebir kopyası + volumetrik bileşen). MonsterAttackFeedback.swingLight → TopLight. Mekan_3 importer: importLights = false; eski ışığa eklenmiş bileşen override'ları kaldırıldı, boş `Light` düğümü gizli. Işıklar artık hep Lights grubunda: TopLight, DragonLight, ShelfLightL, ShelfLightR.

---

# Feedback satırı yerleşimi
- Sorun: satır (z 1.2) kum saatinin (z 0.06-0.50) arkasında; 5. kare saklanıyordu.
- Kullanıcı A'yı seçti: FeedbackRow (-0.05, 2.02, 1.2), x -20°, ölçek 1.3 → 5'li satırın sağ kenarı ekranın %61.1'i, kum saati %62.6'dan başlıyor (Play'de gerçek serviste doğrulandı).
- FeedbackView: Layout (spacing, tileScale, miniSize 0.2), Colors, Editor Preview (previewTiles = 5: Play dışında Scene view'da örnek kare ve minyatür alanını gizmo ile çizer; sahneye obje eklemez).

---

# 16:9 sabit görüntü
- Editör Game view zaten 16:9'du; build'de Mac tam ekran (monitör oranı, MacBook 16:10), WebGL 960x600 (16:10), pencere 1024x768 (4:3) idi → 16:10'da sol üst raf tabağı %6 kırpılıyordu.
- AspectRatioLock (Main Camera): ekran oranı ne olursa olsun kamera görüntüsü 16:9; fazla alan siyah bant. Pencere boyutu değişince yeniden hesaplar.
- LetterboxCamera: depth -10, hiçbir şey çizmeyen, ekranı siyaha temizleyen arka kamera (URP viewport dışını temizlemiyor).
- Player Settings: varsayılan pencere 1920x1080, WebGL 960x540.
- Doğrulama: 16:10 simülasyonunda üst/alt 45 px siyah bant, iç görüntü 1440x810 (1.778), kırpılma yok, sis düzgün.

---

# Toz parçacıkları
- `Prefabs/DustMotes` (sahnede kökte, (0, 2.6, 0.6)): kutu şekli 7x4x5, ömür 8-14 sn, boyut 0.012-0.028, emisyon 25/sn (~270 canlı), max 400, noise 0.05 / 0.3, çok hafif yükselme, prewarm.
- `Materials/M_Dust`: URP Particles/Lit, saydam, yumuşak yuvarlak doku; renderer normal yönü küresel (0) → toz sadece ışık huzmelerinde parlar.
- Kullanıcı ayarları prefab'a uygulandı: boyut 0.024, emisyon 24, renk gri %64, alan 8x4x7 (2 birim geride), noise frekansı 0.2, ömür boyunca küçülme açık.
- Play'e girerken görülen "You can only call GUI functions from inside OnGUI" hatası Unity'nin ParticleSystem Inspector'ından (DustMotes seçiliyken Play); oyunu etkilemiyor.
- Toz etrafındaki kare: M_Dust'ta "Preserve Specular Lighting" açılmıştı (premultiplied blend; ışığın bir kısmı alfa ile çarpılmadan tüm kareye ekleniyor). Kapatıldı → düz alfa blend (SrcAlpha / OneMinusSrcAlpha). Büyük test parçacığıyla doğrulandı: açıkken kare görünüyor, kapalıyken sadece yuvarlak.

---

# Mekan_4
- FBX yerinde güncellendi: Mekan_3.fbx → Mekan_4.fbx (aynı GUID; importLights/importCameras kapalı kaldı).
- Yeni: `fork` (masada, tabağın sağı), masa adı `table` (MonsterAttackFeedback.table → table), arka duvar 7.28 m.
- Kum saati: küçültülüp masanın sol önüne alınmış (55° dönük). Prefab Body yeni parçalardan yeniden kuruldu; Blender'da tabanı masanın 6.8 cm üstündeydi → Hourglass kökü 6.8 cm indirildi (taban = masa üstü 1.653).
- Tabak materyali yeni dosyadan; tabak kullanıcının Unity'deki yerinde (z -0.65; Blender z -0.43).
- Texture'lar FBX'e gömülü değil; Blender kullanıcısının bilgisayarındaki dosyalara referans (lacquered_cherry_wood_diff_1k.jpg, velour_velvet_diff_1k.jpg, forest_leaves_02_arm_1k.jpg, farm_furrows_nor_dx_1k.png). Materyaller düz renk/beyaz → malzemelerde tintModel açık kalıyor.
- Oda: rafların üstündeki iki üst köşe hâlâ açık (%3.2); yan duvar/tavan gerekiyor.

---

# Mekan_5 (texture'lı) + ejderha rig'i
- FBX yerinde güncellendi: Mekan_4.fbx → Mekan_5.fbx (aynı GUID). Gömülü texture'lar `Models/Textures`'a çıkarıldı (materialSearch Everywhere). `buyuktabak-tx` uzantısızdı → `.png` yapıldı, tabağa `Materials/M_Plate` ile verildi.
- Texture'lı malzemelerde tintModel kapalı (ing1, 5, 6, 8, 9), diğerlerinde açık. ing5 ve ing9 aynı yeşil yaprak texture'ı (feedback yeşiline yakın) — kullanıcıya bildirildi.
- `Models/Dragon.fbx` (ejderha.fbx): 5 kemik (Bone → .001 → .002 → .003 / .004), Ejderha skinned (146 ağırlıksız köşe), Disler ve Sapka kemiğe bağlı değil, animasyon yok, ~11 m. Plan kullanıcı onayı bekliyor.

### Ejderha rig – durum (2026-11-07)
- [x] Dragon.fbx prefab'a yerleşti, greybox kafa/boyun ve DragonGreyboxAnimator silindi
- [x] Diş/şapka ParentConstraint ile Bone.003'e bağlı
- [x] DragonRigAnimator: idle + IK ısırma + çene; Play testinde çene doğru yöne (aşağı) açılıyor
- [x] Isırma: temas anında ağız hedefe ~17 cm; gövde ~0.65 öne kayıyor. IK'ya `headTurn` (0.4) eklendi, kafa oyuncuya dönük kalıyor, boyun daha çok eğiliyor
- [x] Diş/şapka ısırma sırasında Bone.003'e göre yerinde (ölçüldü)
- [x] Oyuncuyu yeme testi: ağız kameraya ~0.3, gövde öne kayıyor
- [x] DragonRigAnimator sadeleştirildi (Bite sınıfı, ikWeight ile geri dönüş), DragonTestKeys: N tabak, M oyuncu ısırığı (editor/dev build)
- [x] FeedbackRow kullanıcı tarafından z 0.5'e alınmış; DialogText y 3.85 (şapkanın üstünde)
- [x] ejderha2.fbx (diş+şapka mesh'e birleşik ve skinli) Dragon.fbx'in yerine konuldu, GUID korundu; ParentConstraint'ler kalktı, Mouth yeniden ortalandı
- [x] 146 ağırlıksız vertex: gövdenin arka/alt kısmı (z≈3.3–3.8), Bone'a gidiyor, kafa/boyunda yok
- [x] Dişler düzeldi (ejderha1.fbx 16:55, sivri dişler dudakla aynı ağırlıkta, 45° çene açıkken yerinde)
- [ ] Blender: şapka hâlâ %100 Bone.002 (boyun), Bone.003 olmalı

## Çatal + yazıya bağlı bitişler (2026-11-07, onaylandı)
- Çatal: `Fork` IngredientData (Mekan fork modeli), GameConfig.fork (tariflere girmez). Pantry tıklanabilir yapar, Mekan'daki fork spot olur. Tabakta slot kaplar, geri alınabilir.
- Çatallı servis: Plate Bite → yeni `ForkEaten` durumu → Rage (geri/yukarı kalkış + yazı bitene kadar kükreme) → yazı biter → afterLineDelay (0.3) → Player Bite → değdiği an bitiş ekranı.
- TimeUp/Final: yazı biter → afterLineDelay → Player Bite → bitiş ekranı (sabit 1.6/2.1 sn beklemeler kalkıyor).
- TimeUp: kum saati hafif kalkar, ısırık anına kadar gergin titrer, ısırıkta masaya düşer.
- Isırıklarda çatal da sallanır (MonsterAttackFeedback). Test tuşları (TestKeys, Game objesinde): N tabak, M oyuncu, V rage, B süreyi bitirir.
- Metinler: "A *fork*?! Then I'll have the hand that holds it." / "You fed the dragon a fork."

- [x] Kod  - [x] Sahne bağlantıları  - [x] Play testi (çatal: yenme 1.4 sn, yazı 5.3'te biter, ekran 6.5'te; süre bitti: kum saati ~5 cm kalkıp titriyor, ısırıkta düşüyor)

## Run süresi + kum saati gerginliği (2026-11-07, onaylandı)
- `RunTimer` (Game objesi): bütün run için tek süre, runDuration 200 sn (Inspector). Süre hiç durmaz (servis/geçiş dahil), sadece bitişlerde durur. Bitince GameController servisi keser ve TimeUp'a geçer. Müzik hızlanması ileride bunun oranına bağlanacak.
- GameConfig.dishDuration kalktı.
- Kum saati hiç dönmez; kum run boyunca akar. Gerginlik kalan süreye göre aşamalar (Inspector): %80 hafif titreme, %50 havaya kalkar, %20 çıldırır, %0 en çok. %0'da masaya düşme yok (Drop kalktı).
- B test tuşu RunTimer'ı bitirir.

- [x] Kod  - [x] Sahne  - [x] Play testi (%80 hafif titreme, %50'de 3 cm kalkış, %20'de ~5 cm ve sert, %0'da ~6 cm; TimeUp sonrası düşmeden çıldırmaya devam; restart süreyi sıfırlıyor)

## Ses sistemi (2026-11-07, onaylandı)
- AudioMixer `MainMixer`: Master > Music, SFX, Ambience, UI. Kanal sesleri Audio Mixer penceresinden.
- `SoundCue` (ScriptableObject): klip(ler), kanal, ses + rastgelelik, pitch + rastgelelik, gecikme. Yeni ses = yeni cue.
- `SoundManager`: kaynak havuzu, `Play(SoundCue)`; UnityEvent'lerden (ejderha onBite, GameEventHooks) kodsuz bağlanır.
- `MusicPlayer`: jamtasyon loop, RunTimer'a göre hızlanır (hız eğrisi), perde düzeltme 0–1 (Music kanalındaki Pitch Shifter), bitişte yavaşça söner, restart'ta baştan.
- Plate bite: table_hit, fork_rattle, plate_rattle ayrı cue'lar.

- [x] Kod  - [x] Mixer + importlar  - [x] Bağlantılar  - [x] Test (müzik %75'te 1.03x, %50'de 1.13x, %10'da 1.41x; perde düzeltme 1'de shifter 0.71; plate bite 3 ses SFX kanalında; süre bitince 1 sn'de sönüyor)
- [x] Ses bağlantıları SoundManager'da tek listede (Dragon: Plate Bite / Player Bite / Roar; Game: tüm oyun olayları). Ejderhaya PlateBitten/PlayerBitten/Roared olayları eklendi; onBite'taki ses satırları kaldırıldı.
- [x] dragon_eating_1 → PlateBite_DragonEating cue (0.1 sn gecikme), plate bite listesinde.
- [x] plate_rattle -3 dB'ye normalize (orijinal input folder'da). pickup_1-4 / drop_1-2 (Downloads'tan) -3 dB'ye normalize edilip eklendi: Ingredient_Pickup (4 klip, anında) + Ingredient_Drop (2 klip, 0.35 sn = PlateView moveDuration) → SoundManager Ingredient Selected / Removed.
- [x] Drop sesi çıkarıldı (ikisi birden fazlaydı); seçme ve geri almada sadece Ingredient_Pickup çalıyor. Ingredient_Drop cue'su projede duruyor.
- [x] dragon_new_bite (-3 dB'ye normalize) dragon eating'in yerine: cue PlateBite_DragonBite olarak yeniden adlandırıldı (ayarlar korundu). pickup_new eski 4 pickup'ın yerine Ingredient_Pickup'ta tek klip, ±0.05 rastgele.
- [x] Player Lunge (yeni dragon olayı PlayerLunged, oyuncuya atılma başında): PlayerLunge_Dash + PlayerLunge_BoneCrunch (0.91 sn gecikme = dash'in duyulan süresi). Run Loops: Loop_HourglassPour (Ambience, 0.6) run başında başlar, bitişte 1 sn'de söner.

## Ejderha durumları + çiğneme + poke (2026-11-07, onaylandı)
- DragonState önceliği: Idle < Chew < PlateBite < Rage < PlayerBite. Düşük olan yüksek olanı kesemez. Kesilen durumun sesleri 0.1 sn'de söner (SoundManager, Interrupted olayı).
- Chew: plate bite sonrası, yeme sesine göre şekillenmiş çene eğrisi (chomp ~0.35 / ~1.3 sn), baş geri dönerken çiğner. Yeme sesi Chew listesinde (Chew_DragonBite).
- Çatal: plate bite → 1.5x hızlı çiğneme (ses de 1.5x) → Rage → yazı → Player Bite. Player Bite restart'a kadar tutulur.
- Poke: kafaya tıklayınca (Bone.003/ClickArea) kafa tıklanan taraftan 20° kaçar; sadece Idle/Chew'da, çiğnemeyle üst üste çalışır, sesi yok.
- [x] Kod  - [x] Prefab/sahne  - [x] Test (poke yönü, normal servis, çiğnerken süre bitince ses sönmesi, çatal akışı)
- [ ] Çığlık sesi gelince Roar listesine + minRoarDuration = çığlık süresi
- [x] dragon_roar (mp3→wav, -3 dB) → Rage_Roar, Roar listesinde; minRoarDuration 5.78 (kükreme animasyonu sesle birlikte biter). dark_drone (mp3→wav, 75 sn) → Loop_DarkDrone, Run Loops'ta Ambience 0.5.

## Temizlik (2026-11-07)
- Silindi: kullanılmayan sesler (dragon_eating_1, pickup_1-4, drop_1-2, Ingredient_Drop cue), M_Dragon.mat, ShelfView.cs (raf yedeği), PlateView slot işareti, GameEventHooks (bağlantısı yoktu; sesler SoundManager'da), URP template artıkları (SampleScene + build listesi, TutorialInfo, Readme). InputSystem_Actions proje ayarına bağlı olduğu için kaldı.
- Pantry: Mekan'da yeri olmayan malzeme için uyarı verir. SoundManager sadeleştirildi.
- Prefab/sahne/veri dosyaları yeniden kaydedildi (eski alan kayıtları temizlendi). Kayıp script: 0.
- [x] Uçtan uca test: 3 doğru yemek → final, restart, yanlış yemek → çiğneme, çatal → rage → bitiş, süre bitti → bitiş, restart. Konsol: 0 hata / 0 uyarı.
- [x] Run Loops'a süreye bağlı ses çarpanı (volumeAtStart → volumeAtEnd, doğrusal): kum saati 0.95 → 1.2, drone 1 → 1.
- [x] Malzeme uçuşu (PlateView > Move): gidiş/dönüşte kendi dikey ekseninde 1 tam tur (görsel merkez etrafında), kavis yüksekliği ±%30 rastgele, varışta %15 basılıp oturma (0.18 sn).
- [x] Diyalog satırları çok satırlı yazılabilir (TextArea). Final: 'Perfect... but something is still missing -' + alt satırda ortalı '*YOU*!'. Doğru yemekten sonra bir sonraki yemek cümlenin bitmesini bekliyor, sonra nextDishDelay (1.5 sn).
- [x] Diyalogda '|' = yeni cümle (önceki kısım pageHold 0.8 sn kalır, silinir, sonraki ayrı yazılır; IsTyping hepsi bitene kadar true). Final: 'Perfect... but something is still missing|*YOU*!'.
