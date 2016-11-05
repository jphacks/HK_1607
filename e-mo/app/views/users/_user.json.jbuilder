json.extract! user, :id, :login_id, :student_name, :password, :connected_flag, :teacher_flag, :admin_flag, :created_at, :updated_at
json.url user_url(user, format: :json)
