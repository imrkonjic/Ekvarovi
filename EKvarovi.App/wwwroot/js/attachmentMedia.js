window.attachmentMedia = {
  createObjectUrl: (byteArray, contentType) => {
    const bytes = new Uint8Array(byteArray);
    const blob = new Blob([bytes], { type: contentType });
    return URL.createObjectURL(blob);
  },
  revokeObjectUrl: (url) => {
    if (url) {
      URL.revokeObjectURL(url);
    }
  },
  downloadBlob: (byteArray, contentType, fileName) => {
    const bytes = new Uint8Array(byteArray);
    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
};
