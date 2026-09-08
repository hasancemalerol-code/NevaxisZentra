# Email runtime closure — isolated database and port constants
$script:EmailClosureDatabaseName = "crevafactos_email_closure_1"
$script:EmailClosurePristineDatabaseName = "crevafactos_email_pristine_1"
$script:EmailClosurePostgresContainer = "crevafactos-local-postgres-1"
$script:EmailClosurePostgresHost = "127.0.0.1"
$script:EmailClosurePostgresPort = 5433
$script:EmailClosureMigrationUser = "crevafactos"
$script:EmailClosureRuntimeUser = "crevafactos_runtime"
$script:EmailClosureApiPort = 5211
$script:EmailClosureWebPort = 5311
$script:EmailSmtpDoublePort = 19025
$script:EmailSmtpDoublePreviewPort = 19026
