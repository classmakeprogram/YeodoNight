# YeodoNight — Unity 프로젝트 셋업 가이드

이 저장소에는 **C# 스크립트만** 있다. 실제로 돌리려면 Unity 프로젝트를 만들고
스크립트를 넣은 뒤, 아래 순서대로 씬·프리팹·빌드를 구성해야 한다.
학교 축제 부스 시연을 기준으로 정리했다.

---

## 0. 스크립트 목록

| 스크립트 | 종류 | 역할 |
|---|---|---|
| `PlayerController` | 플레이어 | 이동·시점·스태미너·AK·카타나·조준·사망·회복/탄약 획득 |
| `RobotEnemyAI` | 적 | 근접/원거리 AI, NavMeshAgent 추격·공격 |
| `EnemyProjectile` | 적 | 원거리 로봇 투사체 |
| `EnemyTarget` | 적 | 체력·사망, 점수/미션/스포너에 통지 |
| `Hitbox` | 적 | 부위별 데미지 배수 → EnemyTarget 전달 |
| `EnemySpawner` | 매니저 | Mission / Waves 두 모드 스폰(싱글턴) |
| `MissionManager` | 매니저 | 스테이지·미션 3종 순차 진행(싱글턴) |
| `ScoreManager` | 매니저 | 점수·콤보·웨이브·생존시간 + 로컬 랭킹(싱글턴) |
| `GameManager` | 매니저 | 타이틀→플레이→일시정지→결과→타이틀 흐름(싱글턴) |
| `HUD` | UI | 히트마커·데미지 숫자·점수/콤보/웨이브·저체력 효과(싱글턴) |
| `LeaderboardUI` | UI | 결과 화면 이름 입력 + 순위 표시 |

> 모든 매니저는 서로 없어도 동작하도록 null 체크되어 있다. 최소 구성으로 먼저 띄우고
> 하나씩 붙이면 된다.

---

## 1. 프로젝트 생성

- Unity **6000.x (Unity 6) LTS** 또는 **2022.3 LTS**. 템플릿은 **Built-in(3D)** 또는 **URP(3D)** 둘 다 가능
  (트레이서가 쓰는 `Unlit/Color` 셰이더는 두 파이프라인 모두 존재).
- `Assets/Scripts/` 폴더를 만들고 이 저장소의 `.cs` 파일을 전부 넣는다.
- 아트 폴더 구조는 [`3D_MODEL_GUIDELINES.md`](3D_MODEL_GUIDELINES.md) 참고.

## 2. 패키지 (Window ▸ Package Manager)

| 패키지 | 이유 |
|---|---|
| **AI Navigation** (`com.unity.ai.navigation`) | NavMesh 굽기 / NavMeshSurface |
| **Unity UI** (`com.unity.ugui`) | 기본 포함. `Text` / `InputField` 사용 |
| TextMeshPro | 선택. 쓰려면 스크립트의 `Text`를 `TMP_Text`로 교체 필요 |

## 3. 프로젝트 설정 (Edit ▸ Project Settings)

### Tags and Layers
- **Tags** 추가: `Player`, `Enemy`, `HiddenEnemy`
  (`MainCamera`는 기본 존재 — 플레이어 카메라에 지정)

### Player ▸ Active Input Handling
- **`Input Manager (Old)`** 또는 **`Both`** (스크립트는 레거시 `Input` 사용).

### 사용하는 입력 축 (전부 기본값에 존재, 수정 불필요)
`Horizontal`, `Vertical`, `Mouse X`, `Mouse Y`, `Mouse ScrollWheel`, `Jump`

### 조작 요약 (코드 하드코딩)
| 입력 | 동작 |
|---|---|
| WASD / 마우스 | 이동 / 시점 |
| Shift(홀드) | 달리기(스태미너, 전진 중) |
| C | 웅크리기 토글 |
| Q | 구르기 |
| Space | 점프 |
| 좌클릭 | AK 발사 / 카타나 공격 |
| 우클릭(홀드) | AK 정조준 |
| R | 재장전 |
| 휠 / 1 / 2 | 무기 전환 |
| Esc | 일시정지 토글 |
| 아무 키 | 타이틀에서 시작 |

## 4. 씬 계층 구조

```
Scene
├── --- Managers ---            (빈 GameObject, 정리용)
│   ├── GameManager             GameManager
│   ├── ScoreManager            ScoreManager
│   ├── MissionManager          MissionManager   (Waves 전용이면 제외 가능)
│   └── EnemySpawner            EnemySpawner
├── Player                      tag=Player, CharacterController, PlayerController
│   └── Main Camera             tag=MainCamera, Camera, AudioListener
│       ├── AK47_Model          (무기 모델 / 임시 큐브)
│       └── Katana_Model        (평소 비활성)
├── Level                       바닥·벽·엄폐물 (Navigation Static 체크)
├── SpawnPoints
│   ├── Normal_0 … Normal_n     빈 GameObject, NavMesh 위
│   └── Hidden_0
├── Pickups                     (선택) Health/Ammo 프리팹 배치
├── Lighting                    Directional Light 등
├── EventSystem                 (UI 만들면 자동 생성)
└── Canvas                      Render Mode = Screen Space - Overlay
    ├── Panel_Title
    ├── Panel_HUD               HUD 컴포넌트
    │   ├── Text_HP  Text_Ammo  Text_Stamina
    │   ├── Text_Score  Text_Combo  Text_Wave
    │   ├── Text_Mission
    │   ├── Image_Crosshair
    │   ├── Image_HitMarker     (비활성으로 시작)
    │   ├── Image_LowHP         (전체 화면, 붉은색, alpha 0)
    │   └── DamageNumberParent  (빈 RectTransform)
    ├── Panel_Pause             Resume / Quit 버튼
    ├── Panel_GameOver          LeaderboardUI
    └── Panel_Clear             LeaderboardUI
```

### 컴포넌트 · 인스펙터 연결

**Player**
- `CharacterController`: Height 2, Radius 0.4, Center (0, 1, 0)
- `PlayerController`:
  - `playerCam` = Main Camera
  - `akObject` / `katanaObject` = 무기 모델
  - `katanaTrail` = 카타나 블레이드의 TrailRenderer (평소 Emitting off)
  - `hpText` `ammoText` `staminaText` = HUD 텍스트
  - `animator` = 플레이어 Animator (없으면 비워둠 — 애니 없이도 동작)

**GameManager**
- `titlePanel` `hudPanel` `pausePanel` `gameOverPanel` `clearPanel` = 각 패널
- `idleReturnSeconds` = 90 (부스 방치 시 타이틀 복귀), `resultReturnDelay` = 8

**EnemySpawner**
- `mode` = `Mission` 또는 `Waves`
- `enemyPrefab` `hiddenEnemyPrefab`
- `normalSpawnPoints` = SpawnPoints/Normal_* 전부, `hiddenSpawnPoint` = Hidden_0

**HUD** (Panel_HUD에 부착)
- `player` / `cam` 비워도 자동 탐색
- `hitMarker` `damageNumberPrefab` `damageNumberParent` `scoreText` `comboText` `waveText` `lowHpImage`
- `damageNumberPrefab` = 비활성 상태의 Text 오브젝트(씬 밖 또는 프리팹). 폰트/정렬만 맞춰두면 됨

**MissionManager**
- `missionStatusText` = Text_Mission

**LeaderboardUI** (Panel_GameOver, Panel_Clear 각각)
- `nameEntryRow` = InputField+버튼 컨테이너, `nameInput` = InputField, `confirmButton` = 확인 버튼
- `listText` = 순위 출력 Text

### 버튼 OnClick 연결
| 버튼 | 호출 |
|---|---|
| Panel_Pause / Resume | `GameManager.SetPaused(false)` |
| Panel_Pause / Quit | `GameManager.QuitGame()` |
| Panel_GameOver·Clear / 다시하기 (선택) | `GameManager.ReturnToTitle()` |

## 5. 적 프리팹

```
RobotEnemy (tag=Enemy)
├── EnemyTarget        baseHp, deathDelay(사망 애니 길이)
├── RobotEnemyAI       attackStyle = Melee / Ranged
├── NavMeshAgent       Speed 3.5, Stopping Distance 2, Radius/Height 실측
├── Animator           (선택) 파라미터는 3D 가이드라인 7절
├── Hips/Spine/…       스켈레톤
│   ├── Head  (Sphere/Capsule Collider, Hitbox: isHead=✓, mult 3.2)
│   ├── Body  (Capsule Collider,       Hitbox: isHead=✗, mult 1.0)
│   └── Arm/Leg (선택, Hitbox mult 0.7)
└── (Ranged면) Muzzle  빈 Transform, 총구 위치
```
- **큰 루트 콜라이더는 두지 않는다.** 부위별 콜라이더 + `Hitbox`만. (없으면 헤드샷·미션3 불가)
- 콜라이더 `Is Trigger` 해제.
- 숨은 적 프리팹은 같은 구성 + tag `HiddenEnemy` (스포너가 자동 지정도 함).
- **원거리 로봇**: `attackStyle = Ranged`, `projectilePrefab` 연결, `muzzle` 지정, `preferredRange`(기본 12) 조정.

## 6. 투사체 프리팹 (`EnemyProjectile`)

```
Projectile
├── (Sphere mesh, 작게)
├── Sphere Collider   Is Trigger = ✓
├── Rigidbody         Is Kinematic = ✗ (Use Gravity 무관 — 코드가 끔)
└── EnemyProjectile
```

## 7. 픽업 프리팹 (`Pickup`)

```
HealthPickup / AmmoPickup
├── (mesh) — 재생성 쓰면 이 자식을 visual로 지정
├── Collider   Is Trigger = ✓
└── Pickup     kind, amount, respawnSeconds(웨이브 모드면 10~20)
```

## 8. NavMesh 굽기

- 바닥·벽 등 정적 지오메트리에 **Navigation Static** 체크 (또는 `Level` 오브젝트에 `NavMeshSurface` 추가).
- **Window ▸ AI ▸ Navigation ▸ Bake**, 또는 NavMeshSurface의 **Bake**.
- 스폰 포인트가 파란 NavMesh 영역 위에 있는지 확인. 아니면 적이 안 움직인다.

## 9. 애니메이터

- 실제 애니메이션이 아직 없으면 `animator` 필드를 **비워 둔다** — 코드가 알아서 건너뛴다.
- 애니메이션이 준비되면 Animator Controller를 만들고 **3D 가이드라인 7절의 파라미터 이름표**대로
  파라미터·스테이트·트랜지션을 구성한 뒤 필드에 연결.

## 10. 모드 선택 — Mission vs Waves

| | Mission | Waves |
|---|---|---|
| 흐름 | 적5명 → 숨은적 → 헤드샷3명 → 다음 스테이지 (`maxStage`까지) | 전멸 시 다음 웨이브, 무한, 수·체력 증가 |
| 필요 매니저 | MissionManager 필수 | MissionManager 불필요 |
| 종료 | 전 스테이지 클리어 시 `GameManager.OnGameCleared()` | 플레이어 사망으로만 종료 |
| 부스 추천 | 스토리 시연 | **점수 경쟁 시연** (권장) |

`EnemySpawner.mode`로 전환. 부스에서는 `Waves` + `timeBetweenWaves` 3초 + 넉넉한 픽업 배치를 권장.

## 11. 부스 빌드

- **File ▸ Build Settings**: 현재 씬을 Scenes In Build에 추가(인덱스 0). Platform = **Windows / x86_64**.
- **Player Settings**:
  - Fullscreen Mode = Fullscreen Window, 해상도 부스 모니터에 맞춤
  - Default Is Native Resolution = ✓
  - (선택) Visible In Background = ✗, Resizable Window = ✗
- 실행 PC에서 한 판 끝까지 돌려보고: 커서 잠금/해제, 일시정지, 결과→타이틀 자동복귀, 랭킹 저장 확인.
- 랭킹 초기화가 필요하면 `ScoreManager.ClearLeaderboard()` 호출용 임시 버튼을 두거나
  레지스트리/`PlayerPrefs` 삭제.

## 12. 검증 체크리스트 (에디터 Play)

- [ ] 모든 스크립트 컴파일 통과 (이 저장소 상태에서 처음 임포트 시 확인)
- [ ] 타이틀 → 아무 키 → 플레이 전환, 커서 잠김
- [ ] 이동·점프·구르기·웅크리기, 스태미너 소모/회복
- [ ] AK 발사·재장전(예비탄 감소)·정조준 FOV, 휠 무기 전환, 카타나 공격
- [ ] 적: NavMesh 위에서 추격, 근접/원거리 공격이 플레이어 HP를 깎음
- [ ] 헤드/바디 콜라이더 데미지 차이, 헤드샷 시 데미지 숫자 강조
- [ ] 적 처치 시 점수·콤보 증가(HUD)
- [ ] Waves: 전멸 시 다음 웨이브 / Mission: 미션 순차 진행, 스테이지 증가
- [ ] 플레이어 사망 → 게임오버 패널 → 랭킹 입력 → 저장 → 8초 후 타이틀
- [ ] 90초 방치 → 타이틀 복귀
- [ ] 픽업으로 체력·탄약 회복(가득이면 소모 안 됨)
