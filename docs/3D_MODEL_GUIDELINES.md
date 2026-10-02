# YeodoNight 3D 모델 제작·제출 가이드라인

모델링·애니메이션 담당자가 에셋을 Unity 프로젝트에 올릴 때 참고하는 문서다.
목표는 **받자마자 프리팹에 붙여서 바로 도는 상태**로 넘기는 것.

---

## 1. 파일·폴더 규칙

```
Assets/
  1_Script/Main/        게임 스크립트 (아트 담당은 수정 X)
  Art/
    Characters/
      Player/
      RobotEnemy/       현재 Robot_Soldier 에셋이 여기 있음
        Meshes/         RobotEnemy.fbx (메시 + 스켈레톤)
        Animations/     RobotEnemy@Idle.fbx … + RobotEnemy.controller
        Materials/      .mat
        Textures/       .png
        Prefabs/        모델 원본 프리팹(아트 확인용)
    Weapons/
      AK47/             AK47.fbx, Materials/, Textures/
      Katana/
    Props/              픽업·투사체 메시
    _Source/            .blend / .max 원본 (빌드에서 제외)
  Prefabs/              게임에서 실제로 쓰는 프리팹 (Enemy.prefab 등, 스크립트 연결된 것)
  Scenes/               SampleScene.unity
```

- **에셋을 `Assets/` 루트나 저장소 루트에 두지 말 것.** 반드시 위 구조 안에 넣는다.
- `Art/` 아래 프리팹은 "모델만 있는 것", `Assets/Prefabs/` 는 "스크립트·콜라이더까지 붙은 게임용"으로 구분.
- 파일명: `PascalCase`, 공백·한글·특수문자 금지. 예) `RobotEnemy_Walk.fbx`
- 하나의 캐릭터 = 하나의 `.fbx` (메시 + 스켈레톤). 애니메이션 클립은 별도 `.fbx`로:
  `RobotEnemy@Idle.fbx`, `RobotEnemy@Walk.fbx` … (`@` 앞이 같으면 Unity가 같은 릭으로 인식)
- 원본 작업 파일(`.blend` 등)은 `Assets/Art/_Source/` 에 두고 커밋. 텍스처 소스(`.psd`)도 여기.

## 2. 포맷·스케일·좌표

| 항목 | 값 |
|---|---|
| 익스포트 포맷 | FBX (binary, 2018 이상) |
| 단위 | 1 Unity unit = 1 m. Blender면 Scene Scale 1.0, "Apply Scalings: FBX All" |
| 축 | Forward = Z, Up = Y (Unity 기본). Blender FBX 익스포트에서 `-Z Forward, Y Up` |
| 트랜스폼 | 익스포트 전 **Location/Rotation/Scale 모두 Apply** (스케일 1,1,1) |
| 원점(pivot) | 캐릭터: 발밑 바닥 중앙. 무기: 손으로 쥐는 그립 지점 |
| 정면 | 캐릭터가 +Z를 바라보도록 |

넘기기 전에 Unity로 드래그해서 **Scale Factor 1, 크기 실측(사람 ≈ 1.7~1.9 m)** 확인.

## 3. 폴리곤·머티리얼 예산

| 대상 | 삼각형 수 | 머티리얼 수 |
|---|---|---|
| 플레이어(1인칭 팔/무기 위주) | 8k ~ 20k | 1 ~ 2 |
| 로봇 적 | 10k ~ 25k | 1 ~ 2 |
| AK-47 | 4k ~ 10k | 1 |
| 카타나 | 1k ~ 4k | 1 (칼날/손잡이 분리 시 2) |
| 소품 | 상황에 맞게 최소 | 1 |

- 머티리얼은 **Standard** 셰이더 (이 프로젝트는 Built-in 렌더 파이프라인. URP Lit 쓰면 분홍색으로 깨짐). 같은 재질은 머티리얼 하나 공유.
- 실시간 그림자에 안 쓰는 디테일은 노멀맵으로.

## 4. 텍스처

- 워크플로: **PBR Metallic/Roughness**
- 해상도: 캐릭터 2048, 무기 1024, 소품 512~1024. **4K 금지.** 크기는 2의 거듭제곱.
- 맵 종류: `_Albedo`(+알파), `_MetallicSmoothness`, `_Normal`, 필요 시 `_AO`, `_Emission`
  - 로봇 발광부·카타나 스킬 발광은 `_Emission` 사용
- 파일명: `RobotEnemy_Albedo.png` 처럼 `<에셋>_<맵>` 규칙
- 채널 팩: Metallic = R, AO = G(옵션), Smoothness = A 로 통일

## 5. 릭(스켈레톤)

- 플레이어와 로봇 적은 **Humanoid** 릭으로 익스포트 → Import 설정에서 Animation Type = Humanoid, Avatar 생성.
  (Mecanim 리타게팅으로 애니메이션 공유 가능)
- 본 이름은 좌우 명확하게: `Hand_L`, `Hand_R` …
- 버텍스당 본 가중치 **최대 4개**, 실사용 2~3개 권장. 스키닝 안 된 버텍스 없기.
- 스킨드 메시는 가능하면 하나로 병합. 무기 장착용으로 손 본 아래 `WeaponSocket_R` 빈 본 추가.

## 6. 애니메이션 클립

필요 클립(README 협의 기준):

**플레이어**
- Idle, Walk, Run, Crouch_Idle, Crouch_Walk, Slide(슬라이딩)
- Jump_Start / Jump_Air / Jump_Land, Roll
- AK: Fire, Reload, Aim_Idle
- Katana: Attack_1(평타), (여유 시 Attack_2/3 콤보), 스킬 모션
- Hit, Death

**로봇 적**
- Idle, Walk, Run, Crouch_Idle, Crouch_Walk
- Attack, Hit, Death
- ※ 날기 모션은 폐기(협의됨)

규칙
- 30fps, 클립 이름 `PascalCase`
- **Root Motion 끄기** (이동은 코드/NavMesh가 담당). 익스포트 시 루트 본 제자리 고정
- 루프 클립(Idle/Walk/Run 등)은 시작·끝 포즈 일치, Import에서 Loop Time 체크
- 카타나 이펙트: 스킬 모션 트레일 색은 **노랑 → 파랑** 그라데이션 (블레이드 끝 TrailRenderer 머티리얼에서 설정)

## 7. 코드 연동 — 애니메이터 파라미터 (중요)

스크립트가 아래 파라미터를 세팅한다. 애니메이터 컨트롤러에 **정확히 같은 이름**으로 만들고 스테이트에 연결할 것. 없으면 조용히 무시되므로 이름 오타 주의.

### 플레이어 (`PlayerController.cs` → `animator` 필드)

| 이름 | 타입 | 의미 |
|---|---|---|
| `moveSpeed` | Float | 수평 이동 속도 (Idle↔Walk↔Run 블렌드) |
| `isGrounded` | Bool | 접지 여부 |
| `isCrouching` | Bool | 웅크림 토글 |
| `isSprinting` | Bool | 달리기(스태미너 소모 중) |
| `isRolling` | Bool | 구르기 중 |
| `isAiming` | Bool | 우클릭 정조준 중 |
| `weapon` | Int | 0 = AK-47, 1 = Katana |
| `jump` | Trigger | 점프 시작 |
| `roll` | Trigger | 구르기 |
| `shoot` | Trigger | AK 발사 |
| `reload` | Trigger | 재장전 |
| `katanaAttack` | Trigger | 카타나 공격 |
| `hit` | Trigger | 피격 |
| `die` | Trigger | 사망 |

### 로봇 적 (`RobotEnemyAI.cs` + `EnemyTarget.cs`)

| 이름 | 타입 | 의미 |
|---|---|---|
| `moveSpeed` | Float | NavMeshAgent 속도 |
| `isMoving` | Bool | 이동 중 |
| `isRunning` | Bool | 원거리에서 뛰어 접근 |
| `isCrouching` | Bool | 근접 시 웅크려 접근 |
| `attack` | Trigger | 근접 공격 |
| `hit` | Trigger | 피격 |
| `die` | Trigger | 사망 |

## 8. 코드 연동 — 콜라이더 & 피격 부위 (중요)

플레이어 사격은 레이캐스트다. 맞은 콜라이더에서 부위를 판정한다.

1. 적 프리팹 **루트**에 `EnemyTarget` + `RobotEnemyAI` + `NavMeshAgent` + `Animator`.
2. 몸 전체를 감싸는 큰 콜라이더 대신, **본에 맞춘 부위별 콜라이더**를 자식 오브젝트로 둔다:
   - `Head` (머리 본 자식, Sphere/Capsule Collider)
   - `Body` (척추 본, Capsule)
   - `Arm_L/R`, `Leg_L/R` (선택)
3. 각 부위 콜라이더 오브젝트에 **`Hitbox` 컴포넌트**를 붙이고 설정:

| 부위 | `isHead` | `damageMultiplier` |
|---|---|---|
| Head | ✅ | 3.2 |
| Body | ❌ | 1.0 |
| 팔/다리 | ❌ | 0.7 |

- `Hitbox`가 없어도 동작은 한다(루트 `EnemyTarget`으로 몸통 데미지 처리). 단 **헤드샷 판정과 미션 3이 불가능**하므로 최소 `Head` 콜라이더 + `Hitbox`는 필수.
- 콜라이더는 `Is Trigger` 끄기.
- 적 루트 태그: 일반 `Enemy`, 숨은 적 `HiddenEnemy` (스포너가 자동 세팅하지만 프리팹에도 지정 권장).

### 무기 프리팹

- AK-47 프리팹, 카타나 프리팹을 각각 만들어 `PlayerController`의 `akObject` / `katanaObject` 에 연결.
- AK 총구 끝에 빈 오브젝트 `Muzzle` (머즐 플래시·트레이서 시작점).
- 카타나 블레이드 끝~손잡이에 `TrailRenderer` 를 두고 `PlayerController.katanaTrail` 에 연결. 평소 `Emitting = false`.

### 원거리 로봇 / 투사체

- 원거리 로봇: 팔·무기 끝에 빈 오브젝트 `Muzzle` 추가 → `RobotEnemyAI.muzzle` 에 연결.
- 투사체(에너지볼 등): 작은 메시 + 발광 머티리얼. 콜라이더는 코드팀이 트리거로 설정하므로
  모델러는 메시·머티리얼·(선택) 파티클만 제공하면 됨. 권장 지름 0.2~0.4 m.

### 픽업 아이템

- 회복팩 / 탄약상자 저폴리 메시 각 1개. 바닥에 놓이며 회전/부유는 코드/셰이더로 처리 가능하니 모델만.

## 9. Git / 용량

- 바이너리 에셋은 **Git LFS** 사용. 최초 1회:
  ```
  git lfs install
  git lfs track "*.fbx" "*.png" "*.tga" "*.psd" "*.wav" "*.mp3"
  git add .gitattributes
  ```
- `.meta` 파일 **반드시 함께 커밋** (안 하면 참조 다 깨짐).
- `Library/`, `Temp/`, `Logs/`, `Build/` 는 `.gitignore` (커밋 금지).
- 커밋은 에셋 단위로: "로봇 적 Walk/Run 애니메이션 추가" 처럼.

## 10. 에셋 연결 방법 (Unity 작업 순서)

모델을 받은 뒤 게임에서 실제로 돌게 만드는 과정. 코드가 무엇을 기대하는지 기준으로 적었다.

### 10-1. 임포트

1. 1절 폴더 구조에 맞는 위치로 `.fbx` 와 텍스처를 **Unity 에디터 Project 창 안에서** 드래그한다.
   탐색기로 직접 옮기면 `.meta` 가 따라가지 않아 참조가 깨진다. 이미 Unity 안에 있는 에셋을 옮길 때도 Project 창에서.
2. `.fbx` 선택 ▸ Inspector:
   - **Model 탭**: Scale Factor `1`, Convert Units 체크. Apply 후 씬에 드래그해서 키 1.7~1.9 m 확인
   - **Rig 탭**: Animation Type `Humanoid`, Avatar Definition `Create From This Model` ▸ Apply ▸ `Configure…` 에서 본 매핑이 전부 초록인지 확인
     - 애니메이션 전용 `.fbx` (`RobotEnemy@Walk.fbx`)는 Avatar Definition `Copy From Other Avatar` ▸ 본체 fbx의 Avatar 지정
   - **Animation 탭**: 클립별로 Loop Time(루프 클립만), Root Transform Rotation/Position(Y)/Position(XZ) 전부 `Bake Into Pose` 체크 (Root Motion 사용 안 함)
   - **Materials 탭**: `Extract Materials…` ▸ 같은 캐릭터 폴더의 `Materials/` 로 추출
3. 텍스처 Import 설정:
   - `_Normal` ▸ Texture Type `Normal map`
   - `_MetallicSmoothness`, `_AO` ▸ **sRGB 체크 해제**
   - Max Size: 캐릭터 2048, 무기 1024
4. 추출된 머티리얼(`Standard`)에 맵 연결: Albedo ▸ `_Albedo`, Metallic ▸ `_MetallicSmoothness`(Source = Metallic Alpha), Normal Map ▸ `_Normal`, Occlusion ▸ `_AO`, 발광부는 Emission 체크 후 `_Emission`

### 10-2. 로봇 적 프리팹 (`Assets/Prefabs/Enemy.prefab`)

지금 `Enemy.prefab` 은 캡슐 임시 모델이다. 실제 모델로 교체하는 방법:

1. `RobotEnemy.fbx` 를 씬에 드래그 ▸ 이름을 `RobotEnemy` 로 변경. **이 모델 오브젝트가 루트가 된다.**
   - `EnemyTarget` 과 `RobotEnemyAI` 는 `GetComponent<Animator>()` 로 **같은 오브젝트의** Animator만 찾는다.
     모델을 빈 오브젝트의 자식으로 넣으면 애니메이션 파라미터가 전달되지 않는다.
2. 루트에 컴포넌트 추가:

   | 컴포넌트 | 설정 |
   |---|---|
   | `Animator` | Controller = 10-3에서 만든 컨트롤러, Avatar = fbx Avatar, **Apply Root Motion 해제** |
   | `NavMeshAgent` | Speed 3.5, Stopping Distance 2, Radius 0.4~0.5, Height = 실측 키, **Base Offset 0** (피벗이 발밑이므로) |
   | `EnemyTarget` | `baseHp` 80, `deathDelay` = Death 클립 길이(초) |
   | `RobotEnemyAI` | `attackStyle`, `eye` = Head 본 (비우면 1.5 m 높이 사용) |

   - 루트 태그 `Enemy`. **루트에 큰 Capsule Collider를 두지 않는다** (부위 판정이 안 됨).
3. 부위 콜라이더 (본의 자식으로 빈 오브젝트를 만들어 붙인다. 본 자체에 붙여도 됨):

   | 오브젝트 | 부모 본 (Robot_Soldier 기준) | 콜라이더 | `Hitbox` |
   |---|---|---|---|
   | `Head` | `Head` | Sphere, 반지름 ≈ 머리 크기 | `isHead` ✓, `damageMultiplier` 3.2 |
   | `Body` | `Spine1` | Capsule, 몸통 감쌈 | `isHead` ✗, 1.0 |
   | `Arm_L/R`, `Leg_L/R` (선택) | `LeftArm` / `LeftUpLeg` … | Capsule | ✗, 0.7 |

   - 전부 `Is Trigger` 해제. `Hitbox` 는 부모 쪽 `EnemyTarget` 을 자동으로 찾는다.
4. 원거리 로봇이면 총구 끝에 빈 오브젝트 `Muzzle` (위치만 사용, 발사 방향은 코드가 플레이어 쪽으로 계산) ▸ `RobotEnemyAI.muzzle`, `projectilePrefab` = 10-5의 투사체, `attackStyle = Ranged`.
5. 프리팹 저장:
   - **기존 `Enemy.prefab` 교체**: 씬의 `RobotEnemy` 를 Project 창 `Assets/Prefabs/Enemy.prefab` 위로 드래그 ▸ `Replace`.
     스포너 참조(GUID)가 그대로라 추가 연결 불필요.
   - **새 프리팹으로**: `Assets/Prefabs/` 에 드래그해 저장 ▸ 씬 `EnemySpawner` 의 `enemyPrefab` / `hiddenEnemyPrefab` 에 연결.
   - 저장 후 씬에 남은 `RobotEnemy` 는 삭제 (스포너가 생성한다).
6. 숨은 적을 다른 외형으로 하려면 프리팹을 하나 더 만들어 `hiddenEnemyPrefab` 에 연결. 태그·`isHiddenEnemy` 는 스포너가 자동 설정.

### 10-3. 애니메이터 컨트롤러

1. `Art/Characters/RobotEnemy/Animations/` 에서 우클릭 ▸ Create ▸ Animator Controller ▸ `RobotEnemy.controller`
2. Parameters 탭에 7절 표의 파라미터를 **이름·타입 그대로** 추가.
3. 스테이트 구성 (로봇 적 예시):
   - `Locomotion` (기본 스테이트): Blend Tree, 파라미터 `moveSpeed` ▸ 0 = Idle, 3.5 = Walk, 7 = Run
   - `Crouch`: `isCrouching` true 진입 / false 복귀
   - `Attack`, `Hit`: Any State ▸ 트리거(`attack`, `hit`) ▸ 끝나면 Locomotion 복귀 (Has Exit Time)
   - `Death`: Any State ▸ `die`. **나가는 트랜지션 없음**, Any State 트랜지션의 `Can Transition To Self` 해제
4. 루트의 `Animator.Controller` 에 연결.
5. 플레이어는 `PlayerController.animator` 필드에 직접 연결하므로 Animator가 자식(팔 모델)에 있어도 된다.

### 10-4. 무기 (플레이어 1인칭)

씬의 `Main Camera` 아래 `AK47_Temp`, `Katana_Temp` 가 임시 큐브다. 교체 방법:

1. `Art/Weapons/AK47/AK47.fbx` 를 `Main Camera` 의 자식으로 드래그 ▸ 화면 오른쪽 아래에 보이도록 Position/Rotation 조정
2. 총구 끝에 빈 오브젝트 `Muzzle` 추가
3. `player` 의 `PlayerController` ▸ `akObject` 를 새 모델로 교체 ▸ `AK47_Temp` 삭제
   - 무기 전환은 이 오브젝트를 `SetActive` 로 켜고 끄는 방식. **무기 모델에 콜라이더를 두지 말 것** (자기 사격 레이캐스트에 맞음)
4. 카타나도 같은 방식 ▸ `katanaObject`. 칼날 끝에 `TrailRenderer` (Emitting 해제, 6절 색상) ▸ `katanaTrail`
5. 프리팹으로 저장해 두면 다른 씬에서도 재사용 가능: `Assets/Prefabs/Weapons/`

### 10-5. 투사체 · 픽업

- **투사체** (`Assets/Prefabs/EnemyProjectile.prefab`): 루트에 메시, `Sphere Collider`(Is Trigger ✓), `Rigidbody`(Is Kinematic ✗), `EnemyProjectile`. 지름 0.2~0.4 m
- **픽업** (`Assets/Prefabs/HealthPickup.prefab`, `AmmoPickup.prefab`): 루트에 `Collider`(Is Trigger ✓) + `Pickup`(`kind`, `amount`). 메시는 자식으로 두고 `Pickup.visual` 에 연결 (재생성 시 이 자식만 껐다 켬)

### 10-6. 연결 후 확인

1. NavMesh 위에서 적이 추격하는지 (안 움직이면 Base Offset·스폰 위치 확인, `docs/UNITY_SETUP.md` 8절)
2. Animator 창을 열어둔 채 Play ▸ 파라미터 값이 바뀌는지
3. 머리/몸통 사격 시 데미지 숫자 차이 (헤드 = 3.2배)
4. Console에 `Missing` / `The referenced script` 경고 없는지

## 11. 제출 전 체크리스트

- [ ] 트랜스폼 Apply, 스케일 1, Unity에서 실측 크기 정상
- [ ] Import: Scale Factor 1, Animation Type 올바름(Humanoid), Avatar 생성됨
- [ ] 머티리얼 분리 안 깨짐, 텍스처 2의 거듭제곱·규정 해상도
- [ ] 애니메이터 파라미터 이름 = 7번 표와 정확히 일치
- [ ] 적: 루트에 `EnemyTarget`, 최소 `Head` 콜라이더 + `Hitbox(isHead, mult 3.2)`
- [ ] 루프 애니메이션 Loop Time 체크, Root Motion 해제
- [ ] `.meta` 포함해서 커밋, 대용량은 LFS
- [ ] 에셋이 1절 폴더 안에 있음 (`Assets/` 루트·저장소 루트 X)
- [ ] 10절 순서로 프리팹 연결 후 10-6 확인 통과
