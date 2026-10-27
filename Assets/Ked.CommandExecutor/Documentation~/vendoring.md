# 이 폴더는 복사 반입본이다

`Ked.CommandExecutor`는 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소의 `Assets/Ked.CommandExecutor/`와 같은 내용을 담은 한 벌이다(.meta GUID까지).

```
원본   ked-command-executor   (형제 폴더 · Unity 프로젝트 · 실행기의 집)
반입   Assets/Ked.CommandExecutor/   ← 이 폴더
```

사본에서 먼저 고치는 일은 생긴다. 그때는 **원본에도 같은 손을 대서** 두 벌을 다시 맞춘다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-command-executor` · `main` @ `d12e098` |
| 마지막 동기화 | 2026-10-27 · 원본 → 사본 (첫 반입) |
| 맞춘 것 | `Assets/Ked.CommandExecutor/` 전체 |
| 코드 일치 | `.cs`·`.meta`·`.asmdef` 기준 `diff -r` 0건 (아래 "대조" 명령) |

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

## 대조

```
diff -r ../ked-command-executor/Assets/Ked.CommandExecutor Assets/Ked.CommandExecutor --exclude=Documentation~
```
