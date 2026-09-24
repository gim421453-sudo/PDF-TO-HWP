# Visual Fidelity page-background 조사 결과

## 결론

Golden 확보 후 page-level/page-background 이미지 writer를 구현하고
`21-visual-fidelity-a4-page-background.hwpx`를 생성했다. 상태는
`HANCOM_MANUAL_VALIDATION_REQUIRED`이다.

## 확인한 사실

- `19-visual-fidelity-a4-portrait-1page-no-blank.hwpx`와 `20a`~`20e`는
  body에 배치한 full-page 그림이다. 한컴오피스 2024 수동 검증에서 모두
  두 번째 빈 페이지가 발생했으므로 회귀 산출물로만 보존한다.
- `hancom-fullpage-a4-visual-reference.hwpx`에서 `hh:borderFill id="3"`의
  `hc:imgBrush mode="TOTAL"` → `hc:img binaryItemIDRef` 구조를 확인했다.
- 한컴 공개 `hancom-io/hwpx-owpml-model`에는
  `CSectionDefinitionType`의 `pageBorderFill`/`masterPage` 모델과
  `hc:ImgBrush` 타입 식별자가 존재한다. 그러나 모델 선언만으로는 한컴
  2024가 저장하는 이미지 채움의 정확한 XML child 순서, 속성 조합,
  BinData/manifest 연결, 페이지별 적용 규칙을 확정할 수 없다.

## 구현 원칙

Golden XML 전체를 복사하지 않고 기존 package writer가 생성한 구조에
semantic 요소만 추가했다. 기존 body picture 경로는 변경하지 않았고,
sample 21에는 body `hp:pic`를 넣지 않았다.

## 다음 입력

한컴오피스 2024에서 A4 한 페이지에 이미지 배경(쪽 배경/바탕쪽 또는
동등한 page-level 기능)을 설정하고 저장한 HWPX를
`artifacts/reference-hancom/hancom-fullpage-a4-visual-reference.hwpx`로
제공해야 한다. 이후 해당 파일을 template으로 복사하지 않고 XML 모델의
필수 구조와 참조 체인을 추출해 별도 writer/validator를 구현한다.
