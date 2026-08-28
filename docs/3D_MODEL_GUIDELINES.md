# YeodoNight 3D 모델 제작·제출 가이드라인

모델링·애니메이션 담당자가 에셋을 Unity 프로젝트에 올릴 때 참고하는 문서다.
목표는 **받자마자 프리팹에 붙여서 바로 도는 상태**로 넘기는 것.

---

## 1. 파일·폴더 규칙

```
Assets/
  Art/
    Characters/
      Player/           Player.fbx, 텍스처, 머티리얼, 프리팹
      RobotEnemy/       RobotEnemy.fbx, ...
    Weapons/
      AK47/
      Katana/
    Props/
    _Source/            .blend / .max 원본 (빌드에서 제외)
```

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

- 머티리얼은 **URP Lit**(프로젝트가 URP인 경우) 또는 Standard. 같은 재질은 머티리얼 하나 공유.
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

## 10. 제출 전 체크리스트

- [ ] 트랜스폼 Apply, 스케일 1, Unity에서 실측 크기 정상
- [ ] Import: Scale Factor 1, Animation Type 올바름(Humanoid), Avatar 생성됨
- [ ] 머티리얼 분리 안 깨짐, 텍스처 2의 거듭제곱·규정 해상도
- [ ] 애니메이터 파라미터 이름 = 7번 표와 정확히 일치
- [ ] 적: 루트에 `EnemyTarget`, 최소 `Head` 콜라이더 + `Hitbox(isHead, mult 3.2)`
- [ ] 루프 애니메이션 Loop Time 체크, Root Motion 해제
- [ ] `.meta` 포함해서 커밋, 대용량은 LFS
- [ ] 씬에 드래그 → 플레이 → 애니메이션·피격 판정 눈으로 확인
