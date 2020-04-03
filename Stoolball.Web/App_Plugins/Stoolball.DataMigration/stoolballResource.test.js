const stoolballResource = require("./stoolballResource");

const testCases = [
  [
    "__abcdef=d09ddf1330e341e9621db71142c41b5fa3583603697; UMB-XSRF-TOKEN=Not-a-Real-One-uq4RNMdn63cJIxqCsEU9YIyJVMaROja2B9ebQPN47m3b7LxEab85-yPaBOB5G5suyDWs3m7BSf-vt0XCTz4KYPztsd1W-mzVPMUgObCLtBrBe3SHBni9hNqAqec_Rg2; UMB-XSRF-V=-56jZHAt89RuscYlEHQrha7heA60yjuSaK8WiadqO9t8PS7rJe4o905AWQdk7GNbZDQoVNRmBiDUXph3LRu1NpDg1MUzO7wvjAplayo3kdY1",
    "Not-a-Real-One-uq4RNMdn63cJIxqCsEU9YIyJVMaROja2B9ebQPN47m3b7LxEab85-yPaBOB5G5suyDWs3m7BSf-vt0XCTz4KYPztsd1W-mzVPMUgObCLtBrBe3SHBni9hNqAqec_Rg2"
  ],
  [
    "UMB-XSRF-TOKEN=Not-a-Real-One-uq4RNMdn63cJIxqCsEU9YIyJVMaROja2B9ebQPN47m3b7LxEab85-yPaBOB5G5suyDWs3m7BSf-vt0XCTz4KYPztsd1W-mzVPMUgObCLtBrBe3SHBni9hNqAqec_Rg2",
    "Not-a-Real-One-uq4RNMdn63cJIxqCsEU9YIyJVMaROja2B9ebQPN47m3b7LxEab85-yPaBOB5G5suyDWs3m7BSf-vt0XCTz4KYPztsd1W-mzVPMUgObCLtBrBe3SHBni9hNqAqec_Rg2"
  ]
];

describe("stoolballResource", () => {
  it.each(testCases)(
    "should parse an XSRF token from a cookie",
    (cookie, xsrfToken) => {
      const objectUnderTest = stoolballResource();
      const result = objectUnderTest.parseXsrfTokenFromCookie(cookie);

      expect(result).toBe(xsrfToken);
    }
  );
});
