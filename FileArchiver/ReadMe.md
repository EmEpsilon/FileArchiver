# FileZipArchiver

���̃A�v���P�[�V�����́A�w�肵���f�B���N�g�����̃t�@�C���������I�� Zip ���k���A���t�@�C�����폜����c�[���ł��B
�������ȏ�o�߂����t�@�C��������ΏۂƂ��邱�ƂŁA�s�v�ȌÂ��t�@�C�����A�[�J�C�u���A�f�B�X�N�̈�������I�ɗ��p�ł��܂��B

## ��ȋ@�\

1. **Zip ���k**

   - �w�肵������ (DaysOld) �����t�@�C���̍ŏI�X�V�����Â��ꍇ�A�t�@�C���� Zip ���k���Č��t�@�C�����폜�B
   - ���k��� Zip �t�@�C�����ɂ̓^�C���X�^���v (yyyyMMddHHmmss) ���t������A�㏑���Փ˂�h���܂��B

2. **�h���C���� (Dry-run)**

   - `--dry-run` �I�v�V�������w�肷��ƁA���ۂ̃t�@�C������͍s�킸�A�ǂ̃t�@�C�����ΏۂȂ̂��̂݃��O�o�́B

3. **���O�Ǘ��ƃ��[�e�[�V����**

   - �w��T�C�Y (`MaxLogSizeBytes`) �𒴂���ƁA���O�t�@�C���������� Zip ���k���ꃍ�[�e�[�V�����B

4. **�ݒ�t�@�C�� (config.toml)**

   - TOML�`���Őݒ���s���A���k�Ώۂ̓�����t�H���_�ݒ�A���O�o�͐�Ȃǂ𐧌�\�B

## �g����

1. **�w���v�\��**

   ```powershell
   FileZipArchiver.exe --help
   ```

   ��v�ȃI�v�V�����ƊȈՐ�����\�����܂��B

2. **config.toml �̏���**
   �����t�H���_�� `config.toml` ��p�ӂ��A�ȉ��̂悤�Ȑݒ�������܂��B�܂��͓Ǝ��ɕҏW���Ă��������B

   ```toml
   LogFilePath = "log.txt"       # ���O�t�@�C���o�͐�
   LogLevel = "info"             # ���O�o�͏ڍדx (debug/info/warn/error)
   MaxLogSizeBytes = 1048576      # 1MB �Ń��O���[�e�[�V����
   ZipFileNameFormat = "archive_{0:yyyyMMddHHmmss}.zip"  # ���k�t�@�C���̖��O�t�H�[�}�b�g

   [[FolderSettings]]
   Directory = "C:/data"        # �����Ώۃt�H���_
   DaysOld = 30                  # 30���ȏ�O�ɍX�V���ꂽ�t�@�C�������k
   IncludePattern = "\\.log$"   # .log�t�@�C����Ώ�
   ExcludePattern = "^temp"     # temp�Ŏn�܂�t�@�C���͏��O
   Recursive = true              # �T�u�t�H���_���܂߂邩
   ```

3. **�������`�F�b�N**

   ```powershell
   FileZipArchiver.exe --check
   ```

   `config.toml` �̓��e�Ɛ��������`�F�b�N���A�G���[��x��������Ε\�����܂��B

4. **Dry-run**

   ```powershell
   FileZipArchiver.exe --dry-run
   ```

   �t�@�C������͍s�킸�A�����\��t�@�C���݂̂����O�o�́B���S�Ƀe�X�g�ł��܂��B

5. **�{�Ԏ��s**

   ```powershell
   FileZipArchiver.exe
   ```

   �ݒ�t�@�C���ɂ��������āAZIP���k��폜�����{�B���s���e�̓R���\�[���� `log.txt` �ɏo�͂���܂��B

## �I�v�V�����ꗗ

| �I�v�V����           | ����                                                                        |
| --------------- | ------------------------------------------------------------------------- |
| `--help`        | ���̃w���v��\��                                                                  |
| `--check`       | `config.toml` �̓��e�Ɛ��������`�F�b�N                                                |
| `--dry-run`     | ���ۂ̑�����s�킸�A�ǂ̃t�@�C�����ΏۂɂȂ邩���O�o�͂̂�                                             |
| `--version`     | �o�[�W��������\��                                                                |
| `--init-config` | �e���v���[�g `config.toml` ���쐬�B�����ŏo�̓t�@�C�������w��\ (��: `--init-config myconf.toml`) |

## config.toml �ڍאݒ�

| ����                     | ����                                           |
| ---------------------- | -------------------------------------------- |
| **LogFilePath**        | ���O�t�@�C���̕ۑ���p�X                                 |
| **LogLevel**           | ���O�o�͂̏ڍדx�idebug / info / warn / error�j        |
| **MaxLogSizeBytes**    | ���O�t�@�C���T�C�Y������𒴂���Ǝ����� ZIP ���k                  |
| **ZipFileNameFormat**  | ZIP �t�@�C�����̃t�H�[�}�b�g (`{0}` �ɓ������}�������)           |
| **EnableEventLog**     | Windows �C�x���g���O�֏o�͂��邩 (true / false)          |
| **EventLogLevel**      | �C�x���g���O�ւ̏o�͍Œ჌�x���idebug / info / warn / error�j |
| **[[FolderSettings]]** | �����w��B�e�t�H���_���Ƃɍׂ����ݒ肪�\�B�ȉ��͎�ȃp�����[�^��񋓁B        |

### FolderSettings ���̎�ȃp�����[�^

| �p�����[�^                      | �Ӗ�                                          |
| -------------------------- | ------------------------------------------- |
| **Directory**              | �Ώۃt�H���_�̃p�X�i�K�{�j                               |
| **DaysOld**                | �t�@�C���̍X�V��������ȏ�Â��ꍇ��ZIP���k�Ώ�                   |
| **IncludePattern**         | �t�@�C�����𐳋K�\���Ŏw�肵�A�ΏۂɊ܂߂�                       |
| **ExcludePattern**         | �t�@�C�����𐳋K�\���Ŏw�肵�A�Ώۂ��珜�O����                     |
| **Recursive**              | �T�u�t�H���_���܂߂邩 (true/false)                    |
| **EnableRename**           | ���l�[���@�\���g���� (true/false)                     |
| **RenameDaysOld**          | �t�@�C���쐬��������ȏ�Â��ꍇ���l�[���Ώ�                      |
| **RenameOnInUse**          | ���l�[�����A�t�@�C�����b�N�ɑ��������ꍇ�̋��� (`warn` or `error`) |
| **EnableDelete**           | �폜�@�\���g���� (true/false)                       |
| **DeleteDaysOld**          | �t�@�C���쐬��������ȏ�Â��ꍇ�폜�Ώ�                        |
| **DeleteOnInUse**          | �폜���t�@�C�����b�N�ɑ��������ꍇ�̋��� (`warn` or `error`)    |
| **EnableZipCompression**   | Zip���k���s���� (true/false)                      |
| **CreateEmptyAfterRename** | ���l�[����A�����̋�t�@�C�����쐬���邩 (true/false)           |

## ���O���x���ɂ���

- `debug` : �S���O (�f�o�b�O�����܂�)
- `info`  : debug ���������O
- `warn`  : �x�� (warn) �ƃG���[ (error) �̂�
- `error` : �G���[�̂�

## Windows�C�x���g���O�ւ̏o��

`EnableEventLog = true` �ɐݒ肵�A`EventLogLevel` ��K�؂Ɏw�肷��ƁAWindows�� Application �C�x���g���O�ɂ��o�͂��܂��B�������A�ȉ��̐ݒ肪�K�v�ł��F

```powershell
# PowerShell�ȂǂŎ��s�i�Ǘ��Ҍ����j
# "FileArchiver" �Ƃ����\�[�X�� "Application" ���O�ɓo�^
New-EventLog -LogName Application -Source "FileArchiver"
```

�Ǘ��Ҍ������K�v�ƂȂ�ꍇ�����邽�߁A�����ݒ��|���V�[�ɒ��ӂ��Ă��������B

## ���̑�

- `.NET Framework 4.6.2` �ȍ~�œ��삵�܂��B
- ���k��͌ʃt�@�C�� (\*.zip) �ƂȂ�A�Փ˂�����邽�߂� `_yyyyMMddHHmmss` ��t�����Ė������܂��B

