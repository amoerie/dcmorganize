DICOM ORGANIZE
--------------

Organizes DICOM files on your disk.

Options 

```
  files pos. 0               Required. (Default: .) Organize these DICOM files. When missing, this option will be read from the piped input.
  
  -t, --targetDirectory      Required. (Default: .) Organize DICOM files in this directory

  -p, --targetFilePattern    Required. (Default: {PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID}.dcm) Write DICOM files using this pattern. DICOM tags are supported. Fallback for missing DICOM tags are supported. Nested directories will be created on demand.

  --help                     Display this help screen.

  --version                  Display version information.
```