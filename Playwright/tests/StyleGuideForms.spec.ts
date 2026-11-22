import { expect, test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Style guide - forms section", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/style-guide/?alttemplate=styleguideforms");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page, {
      rules: {
        "prefer-native-element": "off", // picks up TinyMCE
        "no-deprecated-attr": "off", // picks up TinyMCE
        "input-missing-label": "off", // not smart enough to recognise aria-label
        "form-dup-name": "off", // Umbraco Forms checkboxes need a hidden "false" input with the same name after the checkbox, because the first value posted is saved
      },
    });
  });

  test("Loads the Umbraco Forms scripts for forms in a block list", async ({
    page,
    request,
  }) => {
    // Forms in a block list are rendered without their scripts, which are rendered at the end of the page
    await expect(page.locator("script#umbraco-forms-collection")).not.toHaveCount(0);

    const umbracoFormsScript = page.locator(
      'script[src*="/App_Plugins/UmbracoForms/assets/themes/default/umbracoforms.js"]',
    );
    await expect(umbracoFormsScript).toHaveCount(1);
    const response = await request.get(
      (await umbracoFormsScript.getAttribute("src")) ?? "",
    );
    expect(response.ok()).toBeTruthy();
  });

  test("Serialises Umbraco Forms conditions in the format its scripts expect", async ({
    page,
  }) => {
    // The stoolball theme's Script.cshtml serialises the form's conditions. Umbraco Forms' scripts read camelCase
    // properties and compare enums as strings, so check that is what is rendered.
    const data = await page
      .locator("script#umbraco-forms-collection")
      .first()
      .getAttribute("data-forms");
    const form = JSON.parse(decodeURI(data ?? ""));
    expect(form.formId).toBeTruthy();
    expect(form.formElementHtmlIdPrefix).toBeDefined();

    const conditions = Object.values(form.fieldConditions) as any[];
    expect(conditions.length).toBeGreaterThan(0);
    for (const condition of conditions) {
      expect(["Show", "Hide"]).toContain(condition.actionType);
      expect(["All", "Any"]).toContain(condition.logicType);
      for (const rule of condition.rules) {
        expect(typeof rule.operator).toBe("string");
        expect(rule.field).toBeTruthy();
      }
    }
  });

  test("TinyMCE keeps links as typed rather than converting them to relative URLs", async ({
    page,
  }) => {
    // TinyMCE initialises asynchronously after DOMContentLoaded, so wait for the editor to exist
    await page.waitForFunction(
      () => (window as any).tinymce?.get("rich-text")?.initialized,
    );

    const links = [
      "https://example.org/page1/page2",
      "/page1/page2",
      "../page2",
    ];

    for (const href of links) {
      const content = await page.evaluate((href) => {
        const editor = (window as any).tinymce.get("rich-text");
        editor.setContent(`<p><a href="${href}">link</a></p>`);
        return editor.getContent();
      }, href);

      expect(content).toBe(`<p><a href="${href}">link</a></p>`);
    }
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(
      page,
      [
        "aria-allowed-role", // picks up TinyMCE
        "button-name", // picks up TinyMCE
        "nested-interactive", // picks up TinyMCE
        "presentation-role-conflict", // picks up TinyMCE
      ],
      [
        "iframe.tox-edit-area__iframe", // TinyMCE's editable area puts aria-label on <body>, which axe flags (aria-prohibited-attr)
      ],
    );
  });
});
