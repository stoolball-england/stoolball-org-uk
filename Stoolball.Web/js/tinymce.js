$(function () {
  tinymce.init({
    selector: "textarea",

    // General options
    theme: "modern",
    plugins: "link lists",
    statusbar: false,
    height: 300,
    menubar: false,
    toolbar: "formatselect bold italic bullist numlist link removeformat",
    block_formats: "Paragraph=p;Heading=h2",
    valid_elements: "p,h2,strong/b,em/i,ul,ol,li,a[href]",
    link_title: false,
    target_list: false,

    // Set appearance of text within editor
    content_css: "/css/tinymce.css",
  });
});
