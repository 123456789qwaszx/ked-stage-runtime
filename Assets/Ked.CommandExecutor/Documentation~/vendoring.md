# 이 폴더는 원본의 한 벌이다

원본은 ked-presentation-runtime의 `Assets/Ked.CommandExecutor/`다. 무대가 실행기를 참조하므로 같은 한 벌을 여기 둔다(.meta GUID까지).
실행기만 따로 보이는 분리본은 ked-command-executor다 — 셋이 모두 같은 내용이다.

```
원본     ked-presentation-runtime/Assets/Ked.CommandExecutor/
한 벌    Assets/Ked.CommandExecutor/   ← 이 폴더
분리본   ked-command-executor/Assets/Ked.CommandExecutor/
```

고치는 곳은 원본이다. 지금 어느 판인지는 원본의 `Assets/Ked.CommandExecutor/Documentation~/vendoring.md`에 적는다.

## 대조

```
diff -r ../ked-presentation-runtime/Assets/Ked.CommandExecutor Assets/Ked.CommandExecutor --exclude=Documentation~
```
