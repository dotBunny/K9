# K9
A collection of functionality useful for automation in Game Development.

## Disclaimer
K9 is by no means the most optimized battle-ready code, nor is it meant to be. It is a finite set of functionality to augment and enhance existing automation and build systems.

## Usages

### K9.Setup
A collection of verbs related to setting up a development environment.

#### CopyFile
The `CopyFile` verb is useful to move files into place when required. A common use of this verb is to make sure a specific editor package is installed on build agents.

##### Arguments
| Short Name | Long Name | Description | Required |
| :--| :-- | :-- | --- | 
| i | input | The full input path to the desired file to copy. Some protocols are support (SMB). | Yes |
| o | output | The full output path where the file should be written too. When using `--extract` this should be the full path to extract the archive into. | Yes |
| c | check | Should the output destination be checked prior to execution, if present return successfully. | No |
| x | extract | If the `--input` is a supported type (Zip), extract the stream to the destination | No |

##### Example
```
K9.Setup.exe CopyFile --input SMB://<username>:<password>@<ip>/<share>/<path>.zip --output c:\output\path --extract --check
```
This will copy the specified zip file (`--input`) from the SMB share, and extract (`--extract`) it to the specified output path (`--output`). It will however early out successfully if the output path already exists (`--check`).

## License
K9 is licensed under the [BSL-1.0 License](https://choosealicense.com/licenses/bsl-1.0/).
> A copy of this license can be found at the root of the project in the `LICENSE` file.