namespace AccountService.Infrastructure.Data;

public static class StoredProcedures
{
    public static string GetAccrueInterestProcedureScript()
    {
        return """
               CREATE OR REPLACE FUNCTION accrue_interest(account_id UUID)
               RETURNS VOID AS $$
               DECLARE
                   account_record RECORD;
                   interest_amount DECIMAL(18,2);
                   days_passed INTEGER;
                   daily_rate NUMERIC;
               BEGIN
                   -- Получаем счёт
                   SELECT * INTO account_record 
                   FROM "Accounts" 
                   WHERE "Id" = account_id 
                     AND "Type" = 1 -- Deposit
                     AND "IsDeleted" = false
                     AND "ClosedAt" IS NULL;

                   IF NOT FOUND THEN
                       RAISE EXCEPTION 'Счёт % не найден', account_id;
                   END IF;

                   IF account_record."Balance" <= 0 THEN
                       RETURN;
                   END IF;

                   IF account_record."InterestRate" IS NULL OR account_record."InterestRate" <= 0 THEN
                       RAISE EXCEPTION 'У счёта % нет процентной ставки', account_id;
                   END IF;

                   -- Кол-во дней с момента открытия
                   days_passed := CASE 
                   WHEN account_record."LastInterestAccrual" IS NULL 
                   THEN (NOW()::DATE - account_record."OpenedAt"::DATE)
                   ELSE (NOW()::DATE - account_record."LastInterestAccrual"::DATE)
                    END;
                   
                   IF days_passed <= 0 THEN
                       RETURN;
                   END IF;
                    
                   
                   -- Расчёт процентов
                   daily_rate := account_record."InterestRate" / 100 / 365;
                   interest_amount := account_record."Balance" * POWER(1 + daily_rate, days_passed) - account_record."Balance";
                   interest_amount := ROUND(interest_amount, 2);

                   IF interest_amount > 0 THEN
                       UPDATE "Accounts" 
                       SET 
                           "Balance" = "Balance" + interest_amount,
                           "LastInterestAccrual" = NOW()
                       WHERE "Id" = account_id;

                       INSERT INTO "Transactions" (
                           "Id", "AccountId", "Amount", "Currency", "Type", 
                           "Description", "Timestamp", "IsCanceled"
                       ) VALUES (
                           gen_random_uuid(),
                           account_id,
                           interest_amount,
                           account_record."Currency",
                           0, -- Credit
                           'Начисление процентов по депозиту',
                           NOW(),
                           false
                       );
                   END IF;
               END;
               $$ LANGUAGE plpgsql;
               """;
    }
}
