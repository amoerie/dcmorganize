📂 DICOM ORGANIZE
--------------

Organizes DICOM files on your disk.

Options 

```
  files pos. 0               Required. (Default: .) Organize these DICOM files. When missing, this option will be read from the piped input.
  
  -t, --targetDirectory      (Default: .) Organize DICOM files in this directory

  -f, --targetFilePattern    (Default: {PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID} - {Guid}.dcm) Write DICOM files using this pattern. DICOM tags are supported. Fallback
                             for missing DICOM tags are supported. Nested directories will be created on demand.

  -a, --targetFileAction     (Default: Move) Action to execute for each file

  -p, --parallelism          (Default: 8) Process this many files in parallel

  --help                     Display this help screen.

  --version                  Display version information.
```
