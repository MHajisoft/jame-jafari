namespace JameJafari.Api.Services;

/// <summary>Server-side image optimization profile (replaces browser processing).</summary>
public enum ImageProcessProfile
{
    /// <summary>Face-centered square crop + 512px JPEG (avatars, person photos).</summary>
    Avatar,

    /// <summary>Document edge detect + perspective crop + 1600px JPEG (invoice/receipt attachments).</summary>
    Document
}
