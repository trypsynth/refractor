# Third-party notices

The Refractor package contains compiled builds of [prism](https://github.com/ethindp/prism). Their license texts are in the package's `licenses/prism` folder and in `native/prism/LICENSES` in the source.

| Component | License | Notes |
| --- | --- | --- |
| prism | MPL-2.0 | Source: https://github.com/ethindp/prism |
| simdutf | Apache-2.0 | Unicode validation and conversion |
| Highway | Apache-2.0 or BSD-3-Clause | |
| {fmt} | MIT | |
| Moderncom | MIT | Windows COM support |
| concurrentqueue | BSD-2-Clause | Simplified BSD, as chosen by prism |
| dr_wav | Public domain | As chosen by prism |
| NVDA controller client RPC definitions | MPL-2.0 | Relicensed to prism from LGPL-2.1, as described in prism's NOTICE |

prism's source is available at the link above, as the Mozilla Public License requires. Refractor builds it without changes, from the commit the `native/prism` submodule points at.
