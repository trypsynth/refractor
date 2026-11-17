# API reference

The <xref:Refractor> namespace is the API most apps need: <xref:Refractor.Prism> to find and create backends, <xref:Refractor.Backend> to speak through one, and <xref:Refractor.CustomBackend> to write your own.

<xref:Refractor.Interop> is prism's C API, one to one, including the plugin ABI types, for anything the safe types do not cover. Each member names the prism function or field it maps to, and [prism's manual](https://github.com/ethindp/prism/tree/master/doc) describes them in full.
