tinymce.init({
    selector: '.myTextarea',
    plugins: 'image paste',
    images_file_types: 'jpg,svg,webp,png',
    //a_plugin_option: true,
    //a_configuration_option: 400,
    paste_data_images: true,
    automatic_uploads: true,
    images_upload_url: '/Question/Upload',
    /* images_upload_base_path: '/UploadedFiles/ImagesUpload',*/
    images_upload_credentials: true,
    relative_urls: false, //convert url -> /Url
    remove_script_host: true,
    statusbar: false,
    promotion: false,
    height: 200,
    resize: true,
    setup: function (editor) {
        editor.on('GetContent', function (e) {
            e.content = e.content.replace(/<p>(&nbsp;|\s)*<\/p>/gi, '');
        });
    }
    //urlconverter_callback: function (url, node, on_save, name) {
    //    console.log(url)
    //    // Do some custom URL conversion
    //    //url = '/'+url.substring(4);

    //    // Return new URL
    //    return url;
    //}
    //images_upload_base_path: '/'
});